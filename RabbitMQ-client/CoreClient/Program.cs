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

    class Program
    {
        private BlockingCollection<Tuple<string, int>> metricsQueue;
        private SettingsManager settings;
        private ClaymoreClient claymoreClient;
        private RabbitProducer rabbitProducer;

        public static void Main(string[] args)
        {
            var pathToSettings = (args.Length == 1) ? args[0] : null;
            var p = new Program(pathToSettings);
            p.Run();
        }

        public Program(string pathToSettings)
        {
            pathToSettings = pathToSettings ?? "settings.json";
            this.metricsQueue = new BlockingCollection<Tuple<string, int>>();
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

        private void Run()
        {
            this.ScheduleClaymoreJob();

            while (!this.metricsQueue.IsCompleted)
            {
                var metric = this.metricsQueue.Take();
                this.rabbitProducer.SendMetric(metric.Item1, metric.Item2)
                    .Subscribe(
                        (val) => Console.WriteLine("Rabbit producer emitted value: {0}", val),
                        (e) => {
                            this.metricsQueue.Add(metric); // try again later
                            Console.WriteLine("Rabbit producer emitted error: {0}", e);
                        },
                        () => Console.WriteLine("Rabbit producer completed sending metric!"));
            }
        }

        private void ScheduleClaymoreJob()
        {
            Console.WriteLine("Scheduling Claymore Job!");
            ISingularity singularity = Singularity.Instance;
            ISchedule schedule = new CronSchedule(settings.ClaymoreSchdeule);
            var job = new SimpleJob(scheduledTime => {
                Console.WriteLine("Executing Claymore Job!");
                this.claymoreClient.requestStats()
                    .Subscribe(
                        (d) => {
                            var tuples = d.ToMetrics("minerrig1");
                            foreach (var tuple in tuples)
                            {
                                this.metricsQueue.Add(tuple);
                            }
                        },
                        (e) => {
                            Console.WriteLine(e.ToString());
                        },
                        () => {
                            Console.WriteLine("Getting claymore metrics completed!");
                        }
                    );
            });
            singularity.ScheduleJob(schedule, job, true);
            singularity.Start();
        }
    }
}
