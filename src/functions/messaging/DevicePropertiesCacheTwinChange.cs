// <copyright file="DevicePropertiesCacheTwinChange.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Functions.Messaging.Shared;

namespace Mmm.Iot.Functions.Messaging
{
    public static class DevicePropertiesCacheTwinChange
    {
        [FunctionName("DevicePropertiesCacheTwinChange")]
        public static async Task Run([EventHubTrigger(eventHubName: "twin-change", Connection = "TwinChangeEventHubConnectionString", ConsumerGroup = "%DevicePropertiesCacheConsumerGroup%")] EventData events, ILogger log)
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
                    log.LogError("Error occurrred Tenant was not set");
                    throw new Exception("Error occurrred Tenant was not set");
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