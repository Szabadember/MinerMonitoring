namespace Entities
{
    public class CoinStats
    {
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
    }
}