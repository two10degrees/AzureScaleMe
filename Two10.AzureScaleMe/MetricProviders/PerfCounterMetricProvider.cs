using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Threading;

namespace Two10.AzureScaleMe.MetricProviders
{
    public class PerfCounterMetricProvider : AbstractMetricProvider
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

        public string CertificateThumbprint { get; private set; }

        public string SubscriptionId { get; private set; }

        public string ServiceName { get; private set; }

        public string RoleName { get; private set; }

        public bool ConfigureCounters { get; private set; }

        public string CounterTableName { get; private set; }

        private static bool init = false;

        protected override double GetValue()
        {
            if (!init)
            {
                Trace.WriteLine("Creating perf counter table");
                PerformanceCounterEntryDataSource.CreateTables(this.CounterTableName);
                init = true;
            }

            if (this.ConfigureCounters)
            {
                // we do this every time, as instances may have transitioned
                Trace.WriteLine("Configuring perf counters");
                Azure.ConfigureDiagnostics(this.CertificateThumbprint, this.SubscriptionId, this.ServiceName, this.StorageConnectionString, this.RoleName, this.SampleRate, 120, new string[] { this.Counter });
            }

            Trace.WriteLine("Retrieving perf counters");
            var ds = new PerformanceCounterEntryDataSource(this.StorageConnectionString);
            var sample = ds.Select(this.SamplePeriod, this.CounterTableName);
            if (!sample.Any())
            {
                throw new AbstractMetricProvider.NoPerfCountersException();
            }
            double avg = sample.Average((a) => double.Parse(a.CounterValue, Thread.CurrentThread.CurrentCulture));
            Trace.WriteLine(string.Format("Average value of '{2}' value = {0} over {1} samples", avg, sample.Count(), this.Counter));
            return avg;
        }


      
    }
}
