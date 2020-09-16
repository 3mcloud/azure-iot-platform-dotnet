// <copyright file="DeviceTelemetry.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Functions.Messaging.Shared;

namespace Mmm.Iot.Functions.Messaging
{
    public static class DeviceTelemetry
    {
        [FunctionName("DeviceTelemetry")]
        public static async Task Run([EventHubTrigger(eventHubName: "telemetry", Connection = "TelemetryEventHubConnectionString", ConsumerGroup = "%DeviceStreamConsumerGroup%")] EventData[] events, ILogger log)
        {
            try
            {
                var cosmosDbRus = Convert.ToInt32(Environment.GetEnvironmentVariable("CosmosDBRus", EnvironmentVariableTarget.Process));
                var cosmosDb = Environment.GetEnvironmentVariable("DeviceStreamDatabaseId", EnvironmentVariableTarget.Process);
                var batchThreshold = Convert.ToInt32(Environment.GetEnvironmentVariable("BatchThreshold", EnvironmentVariableTarget.Process));
                var batchWriteDelay = Convert.ToInt32(Environment.GetEnvironmentVariable("BatchWriteDelay", EnvironmentVariableTarget.Process));
                CosmosOperations docClient = await CosmosOperations.GetClientAsync();
                await docClient.CreateDatabaseIfNotExistsAsync(cosmosDb, cosmosDbRus);

                DeviceService deviceService = new DeviceService();
                await deviceService.ProcessTelemetryAsync(
                    events,
                    log,
                    batchThreshold: batchThreshold == 0 ? 12 : batchThreshold,  // default to 12 if the value is 0. 0 causes an exception
                    batchWriteDelay: batchWriteDelay);
            }
            catch (Exception ex)
            {
                log.LogInformation($"Unable to process telemetry message. Error Message: {ex.Message}");
                throw ex;
            }
        }
    }
}