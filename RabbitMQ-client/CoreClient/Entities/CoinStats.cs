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

        public IEnumerable<Tuple<DateTime, string, string>> ToMetrics(DateTime timestamp, string topicPrefix)
        {
            Func<string, string> keyConverter = (key) => string.Format("{0}.{1}", topicPrefix, key);
            Func<long, string> valueConverter = (x) => string.Format("{0}", x);
            var tupleList = new List<Tuple<DateTime, string, string>>();
            tupleList.Add(new Tuple<DateTime, string, string>(timestamp, keyConverter(KeyNameHashrate), valueConverter(this.Hashrate)));
            tupleList.Add(new Tuple<DateTime, string, string>(timestamp, keyConverter(KeyNameShares), valueConverter(this.Shares)));
            tupleList.Add(new Tuple<DateTime, string, string>(timestamp, keyConverter(KeyNameRejectedShares), valueConverter(this.RejectedShares)));
            tupleList.Add(new Tuple<DateTime, string, string>(timestamp, keyConverter(KeyNameInvalidShares), valueConverter(this.InvalidShares)));
            tupleList.Add(new Tuple<DateTime, string, string>(timestamp, keyConverter(KeyNamePoolSwitches), valueConverter(this.PoolSwitches)));

            return tupleList;
        }
    }
}