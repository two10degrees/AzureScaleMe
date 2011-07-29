using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Two10.AzureScaleMe.MetricProviders;
using Two10.AzureScaleMe.ScalingProviders;

namespace Two10.AzureScaleMe
{

    public enum CompositionStrategy
    { 
        Sum,
        Highest,
        Lowest
    }

    public class RoleMonitor
    {
        public string Name { get; private set; }

        public List<IMetricProvider> MetricProviders { get; private set; }

        public IScalingProvider ScalingProvider { get; private set; }

        public int MinScalingInterval { get; private set; }

        public CompositionStrategy CompositionStrategy { get; private set; }        

        private DateTime? LastScale { get; set; }

        public virtual void Execute()
        {
            var values = new List<int>();
            foreach (var metric in this.MetricProviders)
            {
                try
                {
                    Trace.WriteLine(string.Format("Executing metric: {0}", metric.GetType().Name));
                    int value = metric.GetMetrics();
                    values.Add(value);
                    Trace.WriteLine(string.Format("Metric result: {0}", value));
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString());
                }
            }

            if (values.Count == 0)
            {
                Trace.WriteLine("No metrics provided, exiting");
                return;
            }

            int total = 0;
            switch (this.CompositionStrategy)
            {
                case AzureScaleMe.CompositionStrategy.Sum:
                    total = values.Sum();
                    break;
                case AzureScaleMe.CompositionStrategy.Highest:
                    total = values.Max();
                    break;
                case AzureScaleMe.CompositionStrategy.Lowest:
                    total = values.Min();
                    break;
                default:
                    throw new NotImplementedException(string.Format("Unknown CompositionStrategy {0}", this.CompositionStrategy));
            }


            if (null != this.ScalingProvider && total != 0)
            {
                Trace.WriteLine(string.Format("Scaling by {0} is recommended", total));
                if (!this.LastScale.HasValue || (DateTime.UtcNow - this.LastScale.Value).TotalMinutes >= MinScalingInterval)
                {
                    try
                    {
                        Trace.WriteLine(string.Format("Preparing to scale using '{0}'", this.ScalingProvider.GetType().Name));
                        if (this.ScalingProvider.Scale(total))
                        {
                            this.LastScale = DateTime.UtcNow;
                            Trace.WriteLine("Scale successful");
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.ToString());
                    }
                }
                else
                {
                    Trace.WriteLine(string.Format("Not scaling, as MinScalingInterval hasn't been achieved ({0} < {1})", (DateTime.UtcNow - (this.LastScale.HasValue ? this.LastScale.Value : DateTime.UtcNow)).TotalMinutes, MinScalingInterval));
                }
            }
            else
            {
                Trace.WriteLine("Scaling not recommended, or ScalingProvider is null"); 
            }
        
        }


        public override string ToString()
        {
            return this.Name;
        }
    }
}
