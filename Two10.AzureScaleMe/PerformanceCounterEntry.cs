using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Two10.AzureScaleMe
{
    public class PerformanceCounterEntry : Microsoft.WindowsAzure.StorageClient.TableServiceEntity
    {
        public long EventTickCount { get; set; }
        public string DeploymentId { get; set; }
        public string Role { get; set; }
        public string RoleInstance { get; set; }
        public string CounterName { get; set; }
        public string CounterValue { get; set; }
    }

    public class PerformanceCounterDataContext : TableServiceContext
    {
        public PerformanceCounterDataContext(string baseAddress, StorageCredentials credentials)
            : base(baseAddress, credentials)
        { }

        public IQueryable<PerformanceCounterEntry> PerformanceCounterEntry(string tableName)
        {
             return this.CreateQuery<PerformanceCounterEntry>(tableName);
        }
    }

    public class PerformanceCounterEntryDataSource
    {
        private static CloudStorageAccount storageAccount;
        private PerformanceCounterDataContext context;

        public static void CreateTables(string connectionString, string tableName)
        {
            storageAccount = CloudStorageAccount.Parse(connectionString);
            storageAccount.CreateCloudTableClient().CreateTableIfNotExist(tableName);
            
            /*
            CloudTableClient.CreateTablesFromModel(
                typeof(PerformanceCounterDataContext),
                storageAccount.TableEndpoint.AbsoluteUri,
                storageAccount.Credentials);*/
        }

        public PerformanceCounterEntryDataSource(string connectionString)
        {
            this.context = new PerformanceCounterDataContext(storageAccount.TableEndpoint.AbsoluteUri, storageAccount.Credentials);
            this.context.RetryPolicy = RetryPolicies.Retry(3, TimeSpan.FromSeconds(1));
        }

        public IEnumerable<PerformanceCounterEntry> Select(int periodInMinutes, string tableName)
        {
            var tempResults = (from pc in this.context.PerformanceCounterEntry(tableName)
                               where pc.EventTickCount > DateTime.UtcNow.AddMinutes(-periodInMinutes).Ticks
                               select pc).ToList();

            return tempResults.OrderByDescending(pc => pc.EventTickCount);
        }
    }
}
