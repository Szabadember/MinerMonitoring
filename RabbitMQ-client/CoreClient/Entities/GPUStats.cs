namespace Entities
{
    using System;
    using System.Collections.Generic;

    public class GPUStats: IMetricConvertible
    {
        private readonly string KeyNamePrimaryHashrate = "hashrate";
        private readonly string KeyNameSecondaryHashrate = "secondary_hashrate";
        private readonly string KeyNameFanspeed = "fanspeed";
        private readonly string KeyNameTemperature = "temperature";

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

        public IEnumerable<Tuple<DateTime, string, long>> ToMetrics(DateTime timestamp, string topicPrefix)
        {
            Func<string, string> keyConverter = (key) => string.Format("{0}.{1}", topicPrefix, key);
            var tupleList = new List<Tuple<DateTime, string, long>>();
            tupleList.Add(new Tuple<DateTime, string, long>(timestamp, keyConverter(KeyNamePrimaryHashrate), this.PrimaryHashrate));
            if(this.SecondaryHashrate.HasValue)
            {
                tupleList.Add(new Tuple<DateTime, string, long>(timestamp, keyConverter(KeyNameSecondaryHashrate), this.SecondaryHashrate.Value));
            }
            tupleList.Add(new Tuple<DateTime, string, long>(timestamp, keyConverter(KeyNameFanspeed), this.FanSpeed));
            tupleList.Add(new Tuple<DateTime, string, long>(timestamp, keyConverter(KeyNameTemperature), this.Temperature));

            return tupleList;
        }
    }
}