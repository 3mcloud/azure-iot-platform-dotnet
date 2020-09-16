// <copyright file="DeviceTwin.cs" company="3M">
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

namespace Mmm.Iot.Functions.Messaging
{
    public static class DeviceTwin
    {
        [FunctionName("DeviceTwin")]
        public static async Task Run([EventHubTrigger(eventHubName: "twin-change", Connection = "TwinChangeEventHubConnectionString", ConsumerGroup = "%DeviceStreamConsumerGroup%")] EventData[] events, ILogger log)
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
                    message.SystemProperties.TryGetValue(DeviceTelemetryKeyConstants.IotHubEnqueuedTime, out object dateTimeReceived);
                    var telemetryTimestamp = new TelemetryTimestamp(Convert.ToDateTime(dateTimeReceived));

                    if (tenant != null)
                    {
                        string eventData = Encoding.UTF8.GetString(message.Body.Array);
                        message.SystemProperties.TryGetValue("iothub-connection-device-id", out object deviceId);
                        DeviceService deviceService = new DeviceService();
                        list.Add(Task.Run(async () => await deviceService.SaveDeviceTwinAsync(eventData, Convert.ToString(tenant), deviceId.ToString(), telemetryTimestamp.EpochTimestamp)));
                    }
                    else
                    {
                        log.LogError($"Error occurrred Tenant was not set");
                        exceptionOccurred = true;
                    }
                }
                catch (Exception ex)
                {
                    log.LogError($"Error occurrred : {ex.Message} StackTrace: {ex.StackTrace}  Inner Exception: {(string.IsNullOrEmpty(ex.StackTrace) ? string.Empty : ex.StackTrace)}");
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