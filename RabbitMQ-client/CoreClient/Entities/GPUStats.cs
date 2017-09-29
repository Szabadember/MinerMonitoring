namespace Entities
{
    public class GPUStats
    {
        public long PrimaryHashrate { get; set; }
        public long? SecondaryHashrate { get; set; }
        public int FanSpeed { get; set; }
        public int Temperature { get; set; }

        public override string ToString()
        {
            var primaryString = string.Format("Primary coin's hashrate: {0} kH/s", this.PrimaryHashrate);
            var secondaryString = string.Format("Secondary coin's hashrate: {0} kH/s", this.SecondaryHashrate ?? 0);
            var tempString = string.Format("Temperature: {0}°C", this.Temperature);
            var fanString = string.Format("Fanspeed: {0}%", this.FanSpeed);

            return string.Format("{0}\n{1}\n{2}\n{3}\n", primaryString, secondaryString, tempString, fanString);
        }
    }
}