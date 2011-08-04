using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Two10.AzureScaleMe
{
    public static class ScaleMe
    {

        private static volatile bool active = true;

        public static void Run(int waitPeriod)
        {
            ScaleMe.active = true;
            IList<RoleMonitor> roles = GetRoleMonitors();
            while (ScaleMe.active)
            {
                foreach (var role in roles)
                {
                    try
                    {
                        Trace.WriteLine(string.Format("Executing role monitor: '{0}'", role));
                        role.Execute();
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.ToString());
                    }
                }

                Trace.WriteLine("Finished run - sleeping");
                Trace.WriteLine(string.Empty);
                System.Threading.Thread.Sleep(waitPeriod);
            } 
        }

        public static void InstallCertificates()
        {
            IList<Tuple<string,string>> certificates = Spring.Create<IList<Tuple<string,string>>>("Certificates");
            foreach (var cert in certificates)
            {
                Azure.InstallCertificate(cert.Item1, cert.Item2);
            }

        }

        public static IList<RoleMonitor> GetRoleMonitors()
        {
            return Spring.Create<IList<RoleMonitor>>("RoleMonitors");
        }

        public static void Start(int waitPeriod)
        {
            Action action = new Action(() => { Run(waitPeriod); });
            action.BeginInvoke(null, null);
        }

        public static void Stop()
        {
            ScaleMe.active = false;
        }



    }
}
