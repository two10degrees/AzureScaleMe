using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Diagnostics;

namespace Two10.AzureScaleMe.MetricProviders
{
    public abstract class AbstractMetricProvider : IMetricProvider
    {

        public class NoPerfCountersException : Exception { }

        public double MaxValue { get; set; }

        public double MinValue { get; set; }

        public int MaxThresholdWait { get; set; }

        protected virtual double GetValue()
        {
            throw new NotImplementedException("You must override this method");
        }

        public int GetMetrics()
        {
            double size = 0;
            try
            {
                size = this.GetValue();
            }
            catch (NoPerfCountersException)
            {
                Trace.WriteLine("No performance values returned");
                return 0;
            }

            if (size >= this.MaxValue)
            {
                Trace.WriteLine(string.Format("Has the values crossed the max threshold? YES ({0} >= {1})", size, this.MaxValue));

                // if we haven't recorded this time, let's set it now
                if (!this.UpperThresholdCrossed.HasValue)
                {
                    this.UpperThresholdCrossed = DateTime.UtcNow;
                    this.LowerThresholdCrossed = null;
                }

                if ((DateTime.UtcNow - this.UpperThresholdCrossed.Value).TotalMinutes > this.MaxThresholdWait)
                {
                    // we have broken the max threshold, for the max time, so let's scale up one.
                    Trace.WriteLine(string.Format("Has the threshold wait time been met? YES ({0} > {1}). Recommending scale up", (DateTime.UtcNow - this.UpperThresholdCrossed.Value).TotalMinutes, this.MaxThresholdWait));
                    return 1;
                }
                else
                {
                    Trace.WriteLine(string.Format("Has the threshold wait time been met? NO ({0} <= {1})", (DateTime.UtcNow - this.UpperThresholdCrossed.Value).TotalMinutes, this.MaxThresholdWait));
                }
            }

            if (size <= this.MinValue)
            {
                Trace.WriteLine(string.Format("Has the values crossed the min threshold? YES ({0} <= {1})", size, this.MinValue));

                // if we haven't recorded this time, let's set it now
                if (!this.LowerThresholdCrossed.HasValue)
                {
                    this.LowerThresholdCrossed = DateTime.UtcNow;
                    this.UpperThresholdCrossed = null;
                }

                if ((DateTime.UtcNow - this.LowerThresholdCrossed.Value).TotalMinutes > this.MaxThresholdWait)
                {
                    // we have broken the min threshold, for the max time, so let's scale up one.
                    Trace.WriteLine(string.Format("Has the threshold wait time been met? YES ({0} > {1}). Recommending scale down", (DateTime.UtcNow - this.LowerThresholdCrossed.Value).TotalMinutes, this.MaxThresholdWait));
                    return -1;
                }
                else
                {
                    Trace.WriteLine(string.Format("Has the threshold wait time been met? NO ({0} <= {1})", (DateTime.UtcNow - this.LowerThresholdCrossed.Value).TotalMinutes, this.MaxThresholdWait));
                }
            }

            if (size < this.MaxValue && size > this.MinValue)
            { 
                // we're in the green zone
                Trace.WriteLine(string.Format("Has the value crossed a treshold? NO", size, this.MinValue));
                this.UpperThresholdCrossed = null;
                this.LowerThresholdCrossed = null;
            }
            return 0;
            
        }

        private DateTime? LowerThresholdCrossed { get; set; }

        private DateTime? UpperThresholdCrossed { get; set; }

    }
}
