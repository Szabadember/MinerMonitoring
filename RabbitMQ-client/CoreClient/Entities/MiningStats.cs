namespace Entities
{
    using System;
    using System.Collections.Generic;

    public class MiningStats: IMetricConvertible
    {
        private readonly string KeyNameUptime = "uptime";
        private readonly string KeyNamePrimaryCoin = "primarycoin";
        private readonly string KeyNameSecondaryCoin = "secondarycoin";
        private readonly string KeyNameGPU = "gpu{0}";

        public string MinerVersion { get; set; }
        public int UptimeMinutes { get; set; }
        public CoinStats PrimaryCoin { get; set; }
        public CoinStats SecondaryCoin { get; set; }
        public IEnumerable<GPUStats> GPUStats { get; set; }

        public MiningStats()
        {
            this.GPUStats = new List<GPUStats>();
        }

        public override string ToString()
        {
            var l1 = string.Format("Miner version: {0}", this.MinerVersion);
            var l2 = string.Format("Uptime: {0} minutes", this.UptimeMinutes);
            var l3 = "\nPrimary Coin's stats:";
            var l4 = this.PrimaryCoin != null ? this.PrimaryCoin.ToString() : "off";
            var l5 = "\nSecondary Coin's stats:";
            var l6 = this.SecondaryCoin != null ? this.SecondaryCoin.ToString() : "off";
            var l7 = "\nGPU stats:\n";

            var i = 1;
            foreach (var gpu in this.GPUStats)
            {
                l7 += string.Format("\nGPU #{0}:\n", i++);
                l7 += gpu.ToString() + "\n";
            }

            return string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n", l1, l2, l3, l4, l5, l6, l7);
        }

        public IEnumerable<Tuple<DateTime, string, string>> ToMetrics(DateTime timestamp, string topicPrefix)
        {
            Func<string, string> keyConverter = (key) => string.Format("{0}.{1}", topicPrefix, key);
            Func<long, string> valueConverter = (x) => string.Format("{0}", x);
            var tupleList = new List<Tuple<DateTime, string, string>>();
            tupleList.Add(new Tuple<DateTime, string, string>(timestamp, keyConverter(KeyNameUptime), valueConverter(this.UptimeMinutes)));

            if (this.PrimaryCoin != null)
            {
                var primaryCoinStats = this.PrimaryCoin.ToMetrics(timestamp, keyConverter(KeyNamePrimaryCoin));
                tupleList.AddRange(primaryCoinStats);
            }

            if (this.SecondaryCoin != null)
            {
                var secondaryCoinStats = this.SecondaryCoin.ToMetrics(timestamp, keyConverter(KeyNameSecondaryCoin));
                tupleList.AddRange(secondaryCoinStats);
            }

            var actGPU = 0;
            foreach (var gpu in this.GPUStats)
            {
                var gpuKey = string.Format(KeyNameGPU, actGPU);
                var actGPUStats = gpu.ToMetrics(timestamp, keyConverter(gpuKey));
                tupleList.AddRange(actGPUStats);
                actGPU += 1;
            }

            return tupleList;
        }
    }
}