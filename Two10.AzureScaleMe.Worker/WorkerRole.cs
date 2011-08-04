using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Configuration;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;

namespace Two10.AzureScaleMe.Worker
{
    public class WorkerRole : RoleEntryPoint
    {
        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.WriteLine("Two10.AzureScaleMe.Worker entry point called", "Information");
            AzureScaleMe.ScaleMe.Run(10000);
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;
            if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["DebugAccountName"]) && !string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["DebugAccountKey"]))
            {
                DiagnosticMonitorConfiguration diagnosticConfig = DiagnosticMonitor.GetDefaultInitialConfiguration();
                diagnosticConfig.Logs.ScheduledTransferPeriod = TimeSpan.FromMinutes(1);
                diagnosticConfig.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;

                CloudStorageAccount csa = new CloudStorageAccount(
                    new StorageCredentialsAccountAndKey(
                        ConfigurationManager.AppSettings["DebugAccountName"],
                        ConfigurationManager.AppSettings["DebugAccountKey"]), 
                    true);

                DiagnosticMonitor.Start(csa, diagnosticConfig);
            }
            AzureScaleMe.ScaleMe.InstallCertificates();

            
            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
            return base.OnStart();
        }
    }
}
