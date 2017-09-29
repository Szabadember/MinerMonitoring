namespace Entities
{
    using System.Collections.Generic;

    public class MiningStats
    {
        public string MinerVersion { get; set; }
        public int UptimeMinutes { get; set; }
        public CoinStats PrimaryCoin { get; set; }
        public CoinStats SecondaryCoin { get; set; }
        public IEnumerable<GPUStats> GPUStats { get; set; }

        public override string ToString()
        {
            var l1 = string.Format("Miner version: {0}", this.MinerVersion);
            var l2 = string.Format("Uptime: {0} minutes", this.UptimeMinutes);
            var l3 = "\nPrimary Coin's stats:";
            var l4 = this.PrimaryCoin.ToString();
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
    }
}