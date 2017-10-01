namespace CoreClient
{
    using System;
    using System.IO;
    using Microsoft.Extensions.Configuration;

    public class SettingsManager
    {
        private readonly string SectionNameClaymore = "ClaymoreSettings";
        private readonly string OptionClaymoreHost = "host";
        private readonly string OptionClaymorePort = "port";
        private readonly string OptionClaymoreRetryCount = "retry_count";
        private readonly string OptionClaymoreSchedule = "schedule";
        private readonly string SectionNameMetrics = "MetricsServerSettings";
        private readonly string OptionMetricsURL = "url";
        private readonly string OptionMetricsExchangeName = "exchange_name";
        private readonly string OptionMetricsQueueName = "queue_name";
        private readonly string OptionMetricsTopicPrefix = "topic_prefix";
        private readonly string OptionMetricsRetryCount = "retry_count";

        public long GeneralMaxLogSize { get; set; }
        public string ClaymoreHost { get; private set; }
        public int ClaymorePort { get; private set; }
        public int? ClaymoreRetryCount { get; private set; }
        public string ClaymoreSchdeule { get; private set; }

        public string MetricsURL { get; private set; }
        public string MetricsExchangeName { get; private set; }
        public string MetricsQueueName { get; private set; }
        public string MetricsTopicPrefix { get; private set; }
        public int MetricsRetryCount { get; private set; }

        public SettingsManager(string configFilePath)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(configFilePath);
            var config = builder.Build();
            var claymoreSection = config.GetSection(SectionNameClaymore);
            var metricsSection = config.GetSection(SectionNameMetrics);

            this.ClaymoreHost = claymoreSection[OptionClaymoreHost];
            this.ClaymorePort = int.Parse(claymoreSection[OptionClaymorePort]);
            this.ClaymoreRetryCount = int.Parse(claymoreSection[OptionClaymoreRetryCount]);
            this.ClaymoreSchdeule = claymoreSection[OptionClaymoreSchedule];

            this.MetricsURL = metricsSection[OptionMetricsURL];
            this.MetricsExchangeName = metricsSection[OptionMetricsExchangeName];
            this.MetricsQueueName = metricsSection[OptionMetricsQueueName];
            this.MetricsTopicPrefix = metricsSection[OptionMetricsTopicPrefix];
            this.MetricsRetryCount = int.Parse(metricsSection[OptionMetricsRetryCount]);
        }
    }
}