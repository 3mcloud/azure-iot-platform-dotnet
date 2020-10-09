// <copyright file="DevicePropertiesCacheLifecycle.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Functions.Messaging.Shared;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.Functions.Messaging
{
    public static class DevicePropertiesCacheLifecycle
    {
        [FunctionName("DevicePropertiesCacheLifecycle")]
        public static async Task Run([EventHubTrigger(eventHubName: "lifecycle", Connection = "LifecycleEventHubConnectionString", ConsumerGroup = "%DevicePropertiesCacheConsumerGroup%")] EventData events, ILogger log)
        {
            try
            {
                events.Properties.TryGetValue("tenant", out object tenant);
                if (tenant != null)
                {
                    var cosmosDbRus = Convert.ToInt32(Environment.GetEnvironmentVariable("CosmosDBRus", EnvironmentVariableTarget.Process));
                    var cosmosDb = Environment.GetEnvironmentVariable("DevicePropertiesCacheDatabaseId", EnvironmentVariableTarget.Process);

                    string eventData = Encoding.UTF8.GetString(events.Body.Array);
                    DevicePropertiesCacheService dpService = new DevicePropertiesCacheService();
                    await dpService.UpdateDevicePropertiesAsync(eventData, Convert.ToString(tenant));
                }
                else
                {
                    // This operation is added to take care of the migration of device twins from the ported IoT hubs
                    var document = JObject.Parse(Encoding.UTF8.GetString(events.Body.Array));

                    // The migration tool adds the tenant information while forming the request, we will pull that out of the body and use it for processing the request
                    if (document != null && document["tenant"] != null && !string.IsNullOrEmpty(Convert.ToString(document["tenant"])))
                    {
                        string tenentInfo = document["tenant"].ToString();

                        // once the tenant is pulled out remove it from the request body so that it is not save in the twin information
                        document.Property("tenant").Remove();
                        if (!string.IsNullOrEmpty(tenentInfo))
                        {
                            DevicePropertiesCacheService dpService = new DevicePropertiesCacheService();
                            await dpService.UpdateDevicePropertiesAsync(document.ToString(), Convert.ToString(tenentInfo));
                        }
                    }
                    else
                    {
                        // If the tenant information is still not there in the body we log an exception
                        log.LogError($"Error occurrred Tenant was not set");
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error occurrred : {ex.Message} StackTrace: {ex.StackTrace}  Inner Exception: {(string.IsNullOrEmpty(ex.StackTrace) ? string.Empty : ex.StackTrace)}");
                throw ex;
            }
        }
    }
}