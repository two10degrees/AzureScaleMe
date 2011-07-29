using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using Timers = System.Timers;
using System.Threading;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using Microsoft.Samples.WindowsAzure.ServiceManagement;
using System.Xml.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.Diagnostics.Management;


namespace Two10.AzureScaleMe
{
    static class Azure
    {
        const string NamespaceServiceConfiguration = "http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration";

        const string DeploymentSlot = "Production";

        public static bool UpdateConfiguration(string subscriptionId, string serviceName, string roleName, int delta, string certificateThumbprint, int maxInstanceCount, int minInstanceCount)
        {
            if (string.IsNullOrWhiteSpace(serviceName)) throw new ArgumentNullException("serviceName");
            if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentNullException("roleName");

            string trackingId = null;
            HttpStatusCode? statusCode = null;
            string statusDescription = null;

            try
            {
                var channel = ServiceManagementHelper.CreateServiceManagementChannel("WindowsAzureEndPoint", GetCertificate(certificateThumbprint));

                var deployment = channel.GetDeploymentBySlot(subscriptionId, serviceName, DeploymentSlot);

                if (string.IsNullOrEmpty(deployment.Configuration))
                    throw new InvalidOperationException(string.Format("Cannot change Instance Count for Role {0}. Service Configuration is null or empty", roleName));

                string configXML = ServiceManagementHelper.DecodeFromBase64String(deployment.Configuration);

                if (string.IsNullOrEmpty(configXML))
                    throw new InvalidOperationException(string.Format("Cannot change Instance Count for Role {0}. Failed to Decode Service Configuration.", roleName));

                // Traversing to Role Instance
                XElement serviceConfig = XElement.Parse(configXML, LoadOptions.SetBaseUri);
                XNamespace xmlns = NamespaceServiceConfiguration;
                var currentInstanceCount = (from c in serviceConfig.Elements(xmlns + "Role")
                                            where string.Compare(c.Attribute("name").Value, roleName, true) == 0
                                            select c.Element(xmlns + "Instances").Attribute("count").Value).FirstOrDefault();

                if (currentInstanceCount == null)
                {
                    throw new WebException(string.Format("Failed to read Role Information for Role {0} from Service Configuration", serviceName));
                }

                int curInstanceCount = Int32.Parse(currentInstanceCount);
                Trace.WriteLine(string.Format("Current instance count: {0}", currentInstanceCount));

                int instanceCount = Math.Min(Math.Max(curInstanceCount + delta, minInstanceCount), maxInstanceCount);
                if (curInstanceCount == instanceCount)
                {
                    Trace.WriteLine("Instance count not elligible for adjustment, abandoning");
                    return false;
                }
              
                // Updating instance count
                foreach (XElement p in serviceConfig.Elements(xmlns + "Role"))
                {
                    if (string.Compare((string)p.Attribute("name"), roleName, true) == 0)
                    {
                        p.Element(xmlns + "Instances").Attribute("count").SetValue(instanceCount.ToString());
                    }
                }

                var encodedString = ServiceManagementHelper.EncodeToBase64String(serviceConfig.ToString());

                using (OperationContextScope scope = new OperationContextScope((IContextChannel)channel))
                {
                    try
                    {
                        ChangeConfigurationInput input = new ChangeConfigurationInput { Configuration = encodedString };

                        channel.ChangeConfigurationBySlot(subscriptionId, serviceName, DeploymentSlot, input);

                        if (WebOperationContext.Current.IncomingResponse != null)
                        {
                            trackingId = WebOperationContext.Current.IncomingResponse.Headers[Constants.OperationTrackingIdHeader];
                            statusCode = WebOperationContext.Current.IncomingResponse.StatusCode;
                            statusDescription = WebOperationContext.Current.IncomingResponse.StatusDescription;
                            Trace.WriteLine("Operation ID: {0}", trackingId);
                        }
                    }
                    catch (CommunicationException ce)
                    {
                        ServiceManagementError error = null;
                        HttpStatusCode httpStatusCode = 0;
                        string operationId;
                        ServiceManagementHelper.TryGetExceptionDetails(ce, out error, out httpStatusCode, out operationId);
                        if (error == null)
                        {
                            Trace.WriteLine(ce.Message);
                        }
                        else
                        {
                            Trace.WriteLine(string.Format("HTTP Status Code: {0}", httpStatusCode));
                            Trace.WriteLine(string.Format("Error Message: {0}", error.Message));
                            Trace.WriteLine(string.Format("Operation Id: {0}", operationId));
                        }
                        return false;
                    }
                    finally
                    {
                        if (statusCode != null)
                        {
                            Trace.WriteLine(string.Format("HTTP Status Code: {0}", statusCode));
                            Trace.WriteLine(string.Format("StatusDescription: {0}", statusDescription));
                        }
                    }
                }
            }
            catch (TimeoutException)
            {
                Trace.WriteLine("There was an error processing this command.");
                return false;
            }


            return true;

        }


        public static void ConfigureDiagnostics(string certificateThumbprint, string subscriptionId, string serviceName, string connectionString, string roleName, int sampleRateInSeconds, int transferPeriodInSeconds, string[] counters)
        {
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(connectionString);

            //TODO: Need to remove this if HTTPS is enabled - this allows connecting though http, otherwise the connection will fail.
            DeploymentDiagnosticManager.AllowInsecureRemoteConnections = true;

            //Get the diagnostis manager associated with this blob storage.
            var channel = ServiceManagementHelper.CreateServiceManagementChannel("WindowsAzureEndPoint", GetCertificate(certificateThumbprint));
            var deployment = channel.GetDeploymentBySlot(subscriptionId, serviceName, DeploymentSlot);
            DeploymentDiagnosticManager deploymentDiagnosticsManager = new DeploymentDiagnosticManager(cloudStorageAccount, deployment.PrivateID);

            //Get the Role instance Diagnostics manager for each instance. and use it to enable data collection
            var roleInstanceManagers = deploymentDiagnosticsManager.GetRoleInstanceDiagnosticManagersForRole(roleName);
            RoleInstanceDiagnosticManager.AllowInsecureRemoteConnections = true;
            Trace.WriteLine(string.Format("Getting Diagnostics Managers for Azure Role '{0}'", roleName));

            //Set the new diagnostic monitor configuration for each instance of the role 
            foreach (var ridmN in roleInstanceManagers)
            {
                Trace.WriteLine(string.Format("Enabling counters on instance {0} of role {1}", ridmN.RoleInstanceId, ridmN.RoleName));
                EnableCounters(sampleRateInSeconds, transferPeriodInSeconds, counters, ridmN);
            }
        }


        private static void EnableCounters(int sampleRateInSeconds, int transferPeriodInSeconds, string[] counters, RoleInstanceDiagnosticManager ridmN)
        {
            DiagnosticMonitorConfiguration dmc = ridmN.GetCurrentConfiguration();

            foreach (string counter in counters)
            {
                if (!string.IsNullOrWhiteSpace(counter))
                {
                    PerformanceCounterConfiguration pcc = new PerformanceCounterConfiguration();
                    pcc.CounterSpecifier = counter.Trim();
                    pcc.SampleRate = TimeSpan.FromSeconds(sampleRateInSeconds);
                    dmc.PerformanceCounters.DataSources.Add(pcc);
                    Trace.WriteLine(string.Format("Counter '{0}' Sample Rate {1} seconds", counter, sampleRateInSeconds));
                }
            }

            dmc.PerformanceCounters.ScheduledTransferPeriod = TimeSpan.FromSeconds(transferPeriodInSeconds);
            ridmN.SetCurrentConfiguration(dmc);
        }


        public static int GetQueueSize(string storageConnectionString, string queueName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(queueName);
            queue.CreateIfNotExist();
            int size = queue.RetrieveApproximateMessageCount();
            return size;
        }


        public static X509Certificate2 GetCertificate(string thumbprint)
        {
            X509Store certificateStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            certificateStore.Open(OpenFlags.ReadOnly);

            X509Certificate2Collection certs = certificateStore.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
            if (certs.Count != 1)
            {
                Trace.WriteLine("Client certificate cannot be found. Please check the config file.");
                return null;
            }
            if (!certs[0].HasPrivateKey)
            {
                Trace.WriteLine("Client certificate does not have the private key.");
                return null;
            }
            return certs[0];
        }

    }


}
