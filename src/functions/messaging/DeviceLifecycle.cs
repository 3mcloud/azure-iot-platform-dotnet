// <copyright file="DeviceLifecycle.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Functions.Messaging.Shared;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.Functions.Messaging
{
    public static class DeviceLifecycle
    {
        [FunctionName("DeviceLifeCycle")]
        public static async Task Run([EventHubTrigger(eventHubName: "lifecycle", Connection = "LifecycleEventHubConnectionString", ConsumerGroup = "%DeviceStreamConsumerGroup%")] EventData[] events, ILogger log)
        {
            bool exceptionOccurred = false;
            List<Task> list = new List<Task>();

            try
            {
                var cosmosDbRus = Convert.ToInt32(Environment.GetEnvironmentVariable("CosmosDBRus", EnvironmentVariableTarget.Process));
                var cosmosDb = Environment.GetEnvironmentVariable("DeviceStreamDatabaseId", EnvironmentVariableTarget.Process);
                CosmosOperations docClient = await CosmosOperations.GetClientAsync();
                await docClient.CreateDatabaseIfNotExistsAsync(cosmosDb, cosmosDbRus);
            }
            catch (Exception)
            {
                log.LogError($"Error occurrred while creating Cosmos DB");
                throw;
            }

            foreach (EventData message in events)
            {
                try
                {
                    message.Properties.TryGetValue("tenant", out object tenant);
                    if (tenant != null)
                    {
                        string eventData = Encoding.UTF8.GetString(message.Body.Array);
                        message.SystemProperties.TryGetValue("iothub-connection-device-id", out object deviceId);
                        message.Properties.TryGetValue("opType", out object operationType);

                        if (operationType.ToString().Equals("createDeviceIdentity") || operationType.ToString().Equals("deleteDeviceIdentity"))
                        {
                            DeviceService deviceService = new DeviceService();
                            list.Add(Task.Run(async () => await deviceService.SaveDeviceLifeCycleOperationAsync(eventData, Convert.ToString(tenant), deviceId.ToString(), operationType.ToString())));
                        }
                    }
                    else
                    {
                        // This operation is added to take care of the migration of device twins from the ported IoT hubs
                        var document = JObject.Parse(Encoding.UTF8.GetString(message.Body.Array));

                        // The migration tool adds the tenant information while forming the request, we will pull that out of the body and use it for processing the request
                        if (document != null && document["tenant"] != null && !string.IsNullOrEmpty(Convert.ToString(document["tenant"])))
                        {
                            string tenentInfo = document["tenant"].ToString();
                            string device = document["deviceId"].ToString();
                            log.LogInformation($"{device}");

                            // once the tenant is pulled out remove it from the request body so that it is not save in the twin information
                            document.Property("tenant").Remove();
                            if (!string.IsNullOrEmpty(tenentInfo) && !string.IsNullOrEmpty(device))
                            {
                                DeviceService deviceService = new DeviceService();
                                list.Add(Task.Run(async () => await deviceService.SaveDeviceLifeCycleOperationAsync(document.ToString(), Convert.ToString(tenentInfo), device, "createDeviceIdentity")));
                            }
                        }
                        else
                        {
                            // If the tenant information is still not there in the body we log an exception
                            log.LogError($"Error occurrred Tenant was not set");
                            exceptionOccurred = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.LogError($"Error occurrred in for loop: {ex.Message} StackTrace: {ex.StackTrace}  Inner Exception: {(string.IsNullOrEmpty(ex.StackTrace) ? string.Empty : ex.StackTrace)}");
                    exceptionOccurred = true;
                }
            }

            try
            {
                await Task.WhenAll(list.ToArray());
            }
            catch (Exception ex)
            {
                log.LogError($"Error occurrred : {ex.Message} StackTrace: {ex.StackTrace}  Inner Exception: {(string.IsNullOrEmpty(ex.StackTrace) ? string.Empty : ex.StackTrace)}");
                exceptionOccurred = true;
            }

            if (exceptionOccurred)
            {
                throw new Exception("Function Failed with exception");
            }
        }
    }
}