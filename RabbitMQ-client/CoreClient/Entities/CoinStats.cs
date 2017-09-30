namespace Entities
{
    using System;
    using System.Collections.Generic;

    public class CoinStats: IMetricConvertible
    {
        private readonly string KeyNameHashrate = "hashrate";
        private readonly string KeyNameShares = "shares";
        private readonly string KeyNameRejectedShares = "rejectedshares";
        private readonly string KeyNameInvalidShares = "invalidshares";
        private readonly string KeyNamePoolSwitches = "poolswitches";

        public long Hashrate { get; set; }
        public int Shares { get; set; }
        public int RejectedShares { get; set; }
        public int InvalidShares { get; set; }
        public int PoolSwitches { get; set; }
        public string PoolAddress { get; set; }

        public override string ToString()
        {
            var l1 = string.Format("Hashrate: {0} kH/s", this.Hashrate);
            var l2 = string.Format("Shares: {0}", this.Shares);
            var l3 = string.Format("Rejected shares: {0}", this.RejectedShares);
            var l4 = string.Format("Invalid shares: {0}", this.InvalidShares);
            var l5 = string.Format("Pool switches: {0}", this.PoolSwitches);
            var l6 = string.Format("Pool address: {0}", this.PoolAddress);

            return string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n", l1, l2, l3, l4, l5, l6);
        }

        public IEnumerable<Tuple<string, int>> ToMetrics(string topicPrefix)
        {
            Func<string, string> keyConverter = (key) => string.Format("{0}.{1}", topicPrefix, key);
            var tupleList = new List<Tuple<string, int>>();
            tupleList.Add(new Tuple<string, int>(keyConverter(KeyNameHashrate), (int)this.Hashrate));
            tupleList.Add(new Tuple<string, int>(keyConverter(KeyNameShares), (int)this.Shares));
            tupleList.Add(new Tuple<string, int>(keyConverter(KeyNameRejectedShares), (int)this.RejectedShares));
            tupleList.Add(new Tuple<string, int>(keyConverter(KeyNameInvalidShares), (int)this.InvalidShares));
            tupleList.Add(new Tuple<string, int>(keyConverter(KeyNamePoolSwitches), (int)this.PoolSwitches));

            return tupleList;
        }
    }
}