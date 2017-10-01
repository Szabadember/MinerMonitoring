namespace Entities
{
    using System;
    using System.Collections.Generic;
    
    public interface IMetricConvertible
    {
        IEnumerable<Tuple<DateTime, string, long>> ToMetrics(DateTime timestamp, string topicPrefix);
    }
}