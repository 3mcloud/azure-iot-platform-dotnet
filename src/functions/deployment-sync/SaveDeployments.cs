// <copyright file="SaveDeployments.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Functions.DeploymentSync.Shared;
using Newtonsoft.Json;

namespace Mmm.Iot.Functions.DeploymentSync
{
    public static class SaveDeployments
    {
        [FunctionName("SaveDeployments")]
        public static async Task Run([QueueTrigger("deploymentstosync", Connection = "AzureStorageConnectionString")] string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
            if (!string.IsNullOrEmpty(myQueueItem))
            {
                DeploymentModel deployment = JsonConvert.DeserializeObject<DeploymentModel>(myQueueItem);

                if (deployment != null)
                {
                    DeploymentSyncService service = new DeploymentSyncService();

                    try
                    {
                        await service.UpdateAndSaveDeployment(deployment.TenantId, deployment.Deployment, deployment.Configuration);
                    }
                    catch (Exception)
                    {
                        log.LogError($"Error occurrred while saving deployment");
                        throw;
                    }
                }
            }
        }
    }
}