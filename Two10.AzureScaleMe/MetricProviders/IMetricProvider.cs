using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Two10.AzureScaleMe.MetricProviders
{
    public interface IMetricProvider
    {
        /// <summary>
        /// Implement this method to observe a metric, and return a vote for whether the instance count should be updated.
        /// </summary>
        /// <returns>Return a value (i.e. -1 / 0 / 1) to indicate whether a scale up/down is required. A response of zero indicates that the instance count should not change.</returns>
        int GetMetrics();

    }
}
