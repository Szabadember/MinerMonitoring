namespace CoreClient
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Threading;
    using Chroniton;
    using Chroniton.Jobs;
    using Chroniton.Schedules;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Console;
    using Serilog.Extensions.Logging.File;

    class Program
    {
        private readonly string LoggingCategory = "MetricsClient";
        private readonly string DefaultSettingsFileName = "settings.json";
        private BlockingCollection<Tuple<DateTime, string, long>> metricsQueue;
        private SettingsManager settings;
        private ClaymoreClient claymoreClient;
        private RabbitProducer rabbitProducer;
        private ILogger logger;

        public static void Main(string[] args)
        {
            var pathToSettings = (args.Length == 1) ? args[0] : null;
            var p = new Program(pathToSettings);
            p.Run();
        }

        public Program(string pathToSettings)
        {
            this.logger = new LoggerFactory()
                .AddConsole()
                .AddFile("Logs/log-{Date}.txt", LogLevel.Information, null, false, 10485760)
                .CreateLogger(LoggingCategory);
            this.logger.LogInformation("Metrics forwarder started!");
            this.logger.LogInformation("Path to settings file used: {0}", pathToSettings);

            try
            {
                pathToSettings = pathToSettings ?? DefaultSettingsFileName;
                this.metricsQueue = new BlockingCollection<Tuple<DateTime, string, long>>();
                this.settings = new SettingsManager(pathToSettings);
                this.claymoreClient = new ClaymoreClient(
                    this.settings.ClaymoreHost,
                    this.settings.ClaymorePort,
                    this.settings.ClaymoreRetryCount);
                this.rabbitProducer = new RabbitProducer(
                    this.settings.MetricsURL,
                    this.settings.MetricsExchangeName,
                    this.settings.MetricsQueueName,
                    this.settings.MetricsTopicPrefix,
                    this.settings.MetricsRetryCount);
            }
            catch (Exception e)
            {
                this.logger.LogCritical("Exception caught: {0}", e);
                throw e;
            }
        }

        private void Run()
        {
            this.ScheduleClaymoreJob();

            while (!this.metricsQueue.IsCompleted)
            {
                var metric = this.metricsQueue.Take();
                this.rabbitProducer.SendMetric(metric.Item1, metric.Item2, metric.Item3)
                    .Subscribe(
                        (val) => this.logger.LogWarning("Rabbit producer emitted value: {0}", val),
                        (e) => {
                            this.metricsQueue.Add(metric); // try again later
                            this.logger.LogError("Rabbit producer emitted error: {0}", e);
                            this.logger.LogWarning("Metric \"{0} - {1}:{2}\" put back into the queue!", metric.Item1, metric.Item2, metric.Item3);
                        },
                        () => this.logger.LogInformation("Rabbit producer completed for {0} - {1}:{2}!", metric.Item1, metric.Item2, metric.Item3));
            }
        }

        private void ScheduleClaymoreJob()
        {
            this.logger.LogDebug("Scheduling Claymore Job!");
            ISingularity singularity = Singularity.Instance;
            ISchedule schedule = new CronSchedule(settings.ClaymoreSchdeule);
            var job = new SimpleJob(scheduledTime => {
                this.logger.LogInformation("Executing claymore job!");
                this.claymoreClient.requestStats()
                    .Subscribe(
                        (d) => {
                            try
                            {
                                var utcDateTime = DateTime.UtcNow;
                                var tuples = d.ToMetrics(utcDateTime, this.settings.MetricsTopicPrefix);
                                foreach (var tuple in tuples)
                                {
                                    this.metricsQueue.Add(tuple);
                                }
                                this.metricsQueue.Add(this.InjectWallet(utcDateTime));
                                this.logger.LogInformation("Claymore data queued!");
                            }
                            catch (Exception e)
                            {
                                this.logger.LogError("Failed to process claymore data: {0}", e);
                            }
                        },
                        (e) => {
                            this.logger.LogError("Failed retrieving claymore data: {0}", e);
                        },
                        () => {
                            this.logger.LogInformation("Claymore job completed!");
                        }
                    );
            });
            var scheduledJob = singularity.ScheduleJob(schedule, job, true);
            var nextExecution = schedule.NextScheduledTime(scheduledJob);
            singularity.Start();
            this.logger.LogDebug("Claymore Job scheduled with schedule: {0}!", schedule.ToString());
            this.logger.LogDebug("Claymore Job's next scheduled execution: {0}!", nextExecution);
        }

        private Tuple<DateTime, string, long> InjectWallet(DateTime date)
        {
            var key = string.Format("{0}.wallet", this.settings.MetricsTopicPrefix);
            var walletAddress = long.Parse(this.settings.GeneralWallet);
            var t = new Tuple<DateTime, string, long>(date, key, walletAddress);
            return t;
        }
    }
}
