using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Threading;
using System.Globalization;

namespace Two10.AzureScaleMe.MetricProviders
{
    public class SimplePerfCounterMetricProvider : AbstractMetricProvider
    {
        public string StorageConnectionString { get; private set; }

        /// <summary>
        /// "\Processor(_Total)\% Processor Time"
        /// </summary>
        public string Counter { get; private set; }

        /// <summary>
        /// i.e. 5 seconds
        /// </summary>
        public int SampleRate { get; private set; }

        /// <summary>
        /// i.e. 10 minutes
        /// </summary>
        public int SamplePeriod { get; private set; }

        protected override double GetValue()
        {
     

            Trace.WriteLine("Retrieving perf counters");
            var ds = new PerformanceCounterEntryDataSource(this.StorageConnectionString);
            var sample = ds.Select(this.SamplePeriod, "WADPerformanceCountersTable");
            if (!sample.Any())
            {
                throw new AbstractMetricProvider.NoPerfCountersException();
            }
            double avg = sample.Average((a) => double.Parse(a.CounterValue, CultureInfo.GetCultureInfo("en-US")));
            Trace.WriteLine(string.Format("Average value of '{2}' value = {0} over {1} samples", avg, sample.Count(), this.Counter));
            return avg;
        }


      
    }
}
