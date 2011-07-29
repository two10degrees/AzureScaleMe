using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Diagnostics;

namespace Two10.AzureScaleMe.MetricProviders
{
    public class QueueMetricProvider : AbstractMetricProvider
    {
        public string QueueName { get; private set; }

        public string StorageConnectionString { get; private set; }

        protected override double GetValue()
        {
            int size = Azure.GetQueueSize(this.StorageConnectionString, this.QueueName);
            Trace.WriteLine(string.Format("Size of {0} queue: {1}", this.QueueName, size));
            return (double) size;
        }

    }
}
