namespace Entities
{
    using System;
    using System.Collections.Generic;
    
    public interface IMetricConvertible
    {
        IEnumerable<Tuple<string, int>> ToMetrics(string topicPrefix);
    }
}