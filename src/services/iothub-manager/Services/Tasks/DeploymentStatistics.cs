// <copyright file="DeploymentStatistics.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Hosting;

using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.External.AppConfiguration;
using Mmm.Iot.Common.Services.External.StorageAdapter;
using Mmm.Iot.Common.Services.External.TableStorage;
using Mmm.Iot.IoTHubManager.Services.Models;

using Newtonsoft.Json;

namespace Mmm.Iot.IoTHubManager.Services.Tasks
{
    public class DeploymentStatistics : IHostedService, IDisposable
    {
        private const string DeploymentHistoryPropertiesCollection = "deploymentHistory-{0}_{1}";
        private const string DeploymentDevicePropertiesCollection = "deploymentdevices-{0}";
        private readonly CancellationTokenSource stoppingCts = new CancellationTokenSource();
        private readonly IStorageAdapterClient client;
        private readonly AppConfig config;
        private Task executingTask;

        public DeploymentStatistics(IStorageAdapterClient client, AppConfig config)
        {
            this.client = client;
            this.config = config;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Store the task we're executing
            if (this.executingTask == null)
            {
                this.executingTask = this.ExecuteAsync(this.stoppingCts.Token);
            }

            // If the task is completed then return it,
            // this will bubble cancellation and failure to the caller
            if (this.executingTask.IsCompleted)
            {
                return this.executingTask;
            }

            // Otherwise it's running
            return Task.CompletedTask;
        }

        [SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "I added a timeout value")]
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (this.executingTask == null)
            {
                return;
            }

            try
            {
                // Signal cancellation to the executing method
                this.stoppingCts.Cancel();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(this.executingTask, Task.Delay(5000, cancellationToken));
            }
        }

        public virtual void Dispose()
        {
            this.stoppingCts.Cancel();
            this.stoppingCts.Dispose();
        }

        protected async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Get the connection string from app settings
                string connectionString = this.config.Global.StorageAccountConnectionString;

                // Instantiate a QueueClient which will be used to manipulate the queue
                QueueClient queueClient = new QueueClient(connectionString, "devicetwins");
                if (queueClient.Exists())
                {
                    var devicesChunk = queueClient.ReceiveMessages(20, TimeSpan.FromMinutes(5));
                    while (devicesChunk != null && devicesChunk.Value != null && devicesChunk.Value.Length > 0)
                    {
                        // Receive and process 20 messages
                        QueueMessage[] receivedMessages = devicesChunk;
                        foreach (QueueMessage message in receivedMessages)
                        {
                            byte[] data = Convert.FromBase64String(message.MessageText);
                            string decodedString = Encoding.UTF8.GetString(data);
                            DeviceTwinQueueModel deviceTwinQueueModel = JsonConvert.DeserializeObject<DeviceTwinQueueModel>(decodedString);
                            await this.StoreDevicePropertiesInStorage(deviceTwinQueueModel);

                            // Delete the message
                            queueClient.DeleteMessage(message.MessageId, message.PopReceipt);
                        }

                        devicesChunk = queueClient.ReceiveMessages(20, TimeSpan.FromMinutes(5));
                    }
                }

                await Task.Delay(2000, stoppingToken);
            }
        }

        private async Task StoreDevicePropertiesInStorage(DeviceTwinQueueModel deviceTwinQueueModel)
        {
            if (deviceTwinQueueModel != null)
            {
                if (deviceTwinQueueModel.ExistingDeviceTwin == null)
                {
                    await this.SaveDeviceTwin(deviceTwinQueueModel.DeploymentId, deviceTwinQueueModel.DeviceTwin, null, deviceTwinQueueModel.TenantId);
                }
                else
                {
                    await this.SaveDeviceTwin(deviceTwinQueueModel.DeploymentId, deviceTwinQueueModel.DeviceTwin, deviceTwinQueueModel.ExistingDeviceTwin?.ETag, deviceTwinQueueModel.TenantId);

                    // archive exisiting Device Twin
                    var archiveDeviceTwinValue = JsonConvert.SerializeObject(
                        deviceTwinQueueModel.ExistingDeviceTwin,
                        Formatting.Indented,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                        });
                    await this.client.UpdateAsync(string.Format(DeploymentHistoryPropertiesCollection, deviceTwinQueueModel.DeploymentId, Guid.NewGuid().ToString()), deviceTwinQueueModel.DeviceTwin.DeviceId, archiveDeviceTwinValue, null, deviceTwinQueueModel.TenantId);
                }
            }
        }

        private async Task SaveDeviceTwin(string deploymentId, TwinServiceModel deviceTwin, string existingDeviceTwinEtag, string tenantId)
        {
            var value = JsonConvert.SerializeObject(
                deviceTwin,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                });

            await this.client.UpdateAsync(string.Format(DeploymentDevicePropertiesCollection, deploymentId), deviceTwin.DeviceId, value, existingDeviceTwinEtag, tenantId);
        }
    }
}