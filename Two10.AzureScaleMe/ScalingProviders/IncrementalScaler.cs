using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Two10.AzureScaleMe;
using System.Security.Cryptography.X509Certificates;

namespace Two10.AzureScaleMe.ScalingProviders
{
    public class IncrementalScaler : IScalingProvider
    {
        public int MaxInstances { get; private set; }

        public int MinInstances { get; private set; }

        public string SubscriptionId { get; private set; }

        public string ServiceName { get; private set; }

        public string RoleName { get; private set; }

        public string CertificateThumbprint { get; private set; }

        public bool Scale(int delta)
        {
            if (delta == 0) return false;

            // we don't want a step change of more than 1 in either direction
            delta = Math.Max(Math.Min(1, delta), -1);

            try
            {

                Two10.AzureScaleMe.Azure.UpdateConfiguration(
                    this.SubscriptionId, this.ServiceName, this.RoleName, delta, this.CertificateThumbprint, this.MaxInstances, this.MinInstances);

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                return false;
            }

            return true;
        
        }


    }
}
