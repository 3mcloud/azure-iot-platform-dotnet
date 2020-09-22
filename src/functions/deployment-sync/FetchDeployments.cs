// <copyright file="FetchDeployments.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Azure.Devices;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Functions.DeploymentSync.Shared;
using Mmm.Iot.Functions.DeploymentSync.Shared.Helpers;
using Newtonsoft.Json;

namespace Mmm.Iot.Functions.DeploymentSync
{
    public static class FetchDeployments
    {
        [FunctionName("FetchDeployments")]
        public static async Task Run([QueueTrigger("tenantstosync", Connection = "AzureStorageConnectionString")] string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");

            if (!string.IsNullOrEmpty(myQueueItem))
            {
                TenantQueueItem tenant = JsonConvert.DeserializeObject<TenantQueueItem>(myQueueItem);

                if (tenant != null && !string.IsNullOrWhiteSpace(tenant.TenantId))
                {
                    List<DeploymentServiceModel> deploymentsToSync = new List<DeploymentServiceModel>();
                    IEnumerable<Configuration> deploymentsFromHub = null;
                    try
                    {
                        deploymentsFromHub = await TenantConnectionHelper.GetRegistry(tenant.TenantId).GetConfigurationsAsync(100);

                        DeploymentSyncService service = new DeploymentSyncService();

                        deploymentsToSync.AddRange(await service.GetDeploymentsToSync(tenant.TenantId, deploymentsFromHub));
                    }
                    catch (Exception)
                    {
                        log.LogError($"Error occurrred while fetching deployments");
                        throw;
                    }

                    if (deploymentsToSync != null && deploymentsToSync.Count > 0)
                    {
                        // Get the connection string from app settings
                        string connectionString = Environment.GetEnvironmentVariable("AzureStorageConnectionString", EnvironmentVariableTarget.Process);

                        // Instantiate a QueueClient which will be used to create and manipulate the queue
                        QueueClient queueClient = new QueueClient(connectionString, "deploymentstosync");

                        await queueClient.CreateIfNotExistsAsync();

                        if (queueClient.Exists())
                        {
                            foreach (var deploymentToSync in deploymentsToSync)
                            {
                                DeploymentModel deployment = new DeploymentModel();
                                deployment.TenantId = tenant.TenantId;
                                deployment.Deployment = deploymentToSync;
                                deployment.Configuration = deploymentsFromHub.FirstOrDefault(d => d.Id == deploymentToSync.Id);

                                var deploymentToSyncString = JsonConvert.SerializeObject(deployment);

                                queueClient.SendMessage(Base64Encode(deploymentToSyncString));
                            }
                        }
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