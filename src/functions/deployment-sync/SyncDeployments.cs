// <copyright file="SyncDeployments.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Functions.DeploymentSync.Shared;
using Newtonsoft.Json;

namespace Mmm.Iot.Functions.DeploymentSync
{
    public static class SyncDeployments
    {
        [FunctionName("SyncDeployments")]
        public static async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            TableStorageOperations cloudTableClient = await TableStorageOperations.GetClientAsync();

            TableQuery<TenantModel> query = new TableQuery<TenantModel>().Where(TableQuery.GenerateFilterConditionForBool(nameof(TenantModel.IsIotHubDeployed), QueryComparisons.Equal, true));
            List<TenantModel> tenants = await cloudTableClient.QueryAsync<TenantModel>("tenant", query);

            if (tenants != null && tenants.Count > 0)
            {
                // Get the connection string from app settings
                string connectionString = Environment.GetEnvironmentVariable("AzureStorageConnectionString", EnvironmentVariableTarget.Process);

                // Instantiate a QueueClient which will be used to create and manipulate the queue
                QueueClient queueClient = new QueueClient(connectionString, "tenantstosync");

                await queueClient.CreateIfNotExistsAsync();

                if (queueClient.Exists())
                {
                    foreach (var tenant in tenants)
                    {
                        var tenantMessage = JsonConvert.SerializeObject(new TenantQueueItem(tenant.TenantId));

                        // Send a message to the queue
                        var encodedString = Base64Encode(tenantMessage);
                        queueClient.SendMessage(encodedString);
                    }
                }
            }
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}