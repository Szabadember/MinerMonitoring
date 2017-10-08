namespace CoreClient
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Chroniton;
    using Chroniton.Jobs;
    using Chroniton.Schedules;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Console;
    using RabbitMQ.Client;
    using Serilog.Extensions.Logging.File;

    class Program
    {
        private readonly string LoggingCategory = "MetricsClient";
        private readonly string DefaultSettingsFileName = "settings.json";
        private BlockingCollection<Tuple<DateTime, string, string>> metricsQueue;
        private SettingsManager settings;
        private ClaymoreClient claymoreClient;
        private ILogger logger;

        public static void Main(string[] args)
        {
            var pathToSettings = (args.Length > 0) ? args[0] : null;
            var loglevel = (args.Length > 1 && args[1] == "debug") ? LogLevel.Debug : LogLevel.Information;
            var p = new Program(pathToSettings, loglevel);
            var task = p.Run();
            task.Wait();
        }

        public Program(string pathToSettings, LogLevel loglevel)
        {
            this.logger = new LoggerFactory()
                .AddConsole(loglevel)
                .AddFile("Logs/log-{Date}.txt", loglevel, null, false, 10485760)
                .CreateLogger(LoggingCategory);
            this.logger.LogInformation("Metrics forwarder started!");
            this.logger.LogInformation("Path to settings file used: {0}", pathToSettings);

            try
            {
                pathToSettings = pathToSettings ?? DefaultSettingsFileName;
                this.metricsQueue = new BlockingCollection<Tuple<DateTime, string, string>>();
                this.settings = new SettingsManager(pathToSettings);
                this.claymoreClient = new ClaymoreClient(
                    this.settings.ClaymoreHost,
                    this.settings.ClaymorePort,
                    this.settings.ClaymoreRetryCount);
            }
            catch (Exception e)
            {
                this.logger.LogCritical("Exception caught: {0}", e);
                throw e;
            }
        }

        private async Task Run()
        {
            try
            {
                this.ScheduleClaymoreJob();
            }
            catch (Exception e)
            {
                this.logger.LogCritical("Exception caught: {0}", e);
                throw e;
            }

            while (!this.metricsQueue.IsCompleted)
            {
                var lastTaken = this.metricsQueue.Take();
                try {
                    var factory = new ConnectionFactory();
                    factory.Uri = new Uri(this.settings.MetricsURL);
                
                    using (var conn = factory.CreateConnection())
                    {
                        using (var ch = conn.CreateModel())
                        {
                            ch.ExchangeDeclare(this.settings.MetricsExchangeName, ExchangeType.Topic);
                            var numPublished = 0;
                            do
                            {
                                var timestamp = lastTaken.Item1;
                                var routingKey = lastTaken.Item2;
                                var valueStr = lastTaken.Item3;

                                var props = ch.CreateBasicProperties();
                                var unixTimestamp = (Int64)(timestamp.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                                var messageBodyBytes = System.Text.Encoding.UTF8.GetBytes(valueStr);

                                props.Timestamp = new AmqpTimestamp(unixTimestamp);
                                ch.BasicPublish(this.settings.MetricsExchangeName,
                                            routingKey,
                                            props,
                                            messageBodyBytes);
                                ++numPublished;
                                this.logger.LogDebug("Metric \"{0} - {1}:{2}\" published successfully!", timestamp, routingKey, valueStr);
                                lastTaken = this.metricsQueue.Take();
                            } while (this.metricsQueue.Count > 0);
                            this.logger.LogInformation("{0} metrics published successfully!", numPublished);
                        }
                    }
                }
                catch (Exception e)
                {
                    this.metricsQueue.Add(lastTaken);
                    this.logger.LogError("Error while trying to send metric {0}", e);
                    this.logger.LogWarning("Metric \"{0} - {1}:{2}\" was put back into the queue!", lastTaken.Item1, lastTaken.Item2, lastTaken.Item3);
                    this.logger.LogInformation("Waiting {0} seconds before retrying to send metrics!", this.settings.MetricsRetryDelay);
                    await Task.Delay(this.settings.MetricsRetryDelay * 1000);
                }
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

        private Tuple<DateTime, string, string> InjectWallet(DateTime date)
        {
            var key = string.Format("{0}.wallet", this.settings.MetricsTopicPrefix);
            var tuple = new Tuple<DateTime, string, string>(date, key, this.settings.GeneralWallet);
            return tuple;
        }
    }
}
