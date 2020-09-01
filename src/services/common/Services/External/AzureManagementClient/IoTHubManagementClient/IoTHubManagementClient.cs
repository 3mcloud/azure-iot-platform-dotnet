// <copyright file="IoTHubManagementClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Documents.SystemFunctions;
using Microsoft.Azure.Management.IotHub;
using Microsoft.Azure.Management.IotHub.Models;
using Microsoft.Rest.Azure;
using Microsoft.Rest.ClientRuntime;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Models;

namespace Mmm.Iot.Common.Services.External.Azure
{
    public class IoTHubManagementClient : IIoTHubManagementClient
    {
        private readonly IotHubClient client;
        private readonly AppConfig config;

        public IoTHubManagementClient(IotHubClient client, AppConfig config)
        {
            this.client = client;
            this.config = config;
            this.client.SubscriptionId = this.config.Global.SubscriptionId;
        }

        public async Task<StatusResultServiceModel> StatusAsync()
        {
            try
            {
                var result = this.client.IsDefined();
                await Task.CompletedTask; // Just to keep the signature async, later this should be replaced with more robust status check

                // If the call above does not fail then return a healthy status
                return new StatusResultServiceModel(result, result ? "Alive and well!" : "Undefined IoTHubClient");
            }
            catch (Exception e)
            {
                return new StatusResultServiceModel(false, $"Table Storage status check failed: {e.Message}");
            }
        }

        public async Task DeleteAsync(string iotHubName, CancellationToken token)
        {
            await this.client.IotHubResource.BeginDeleteAsync(this.config.Global.ResourceGroup, iotHubName, token != null ? token : CancellationToken.None);
        }

        public async Task<IotHubDescription> RetrieveAsync(string iotHubName, CancellationToken token)
        {
            return await this.client.IotHubResource.GetAsync(this.config.Global.ResourceGroup, iotHubName, token != null ? token : CancellationToken.None);
        }

        public IPage<SharedAccessSignatureAuthorizationRule> ListKeysAsync(string iotHubName)
        {
            return this.client.IotHubResource.ListKeys(this.config.Global.ResourceGroup, iotHubName);
        }

        public string GetAccessKey(string iotHubName, string name)
        {
            var keys = this.ListKeysAsync(iotHubName);
            return keys.Where(t => t.KeyName == "iothubowner").FirstOrDefault().PrimaryKey;
        }

        public string GetConnectionString(string iotHubName)
        {
            return $"HostName={iotHubName}.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey={this.GetAccessKey(iotHubName, "iothubowner")}";
        }

        public void AddConsumerGroup(string iotHubName, string endpointName, string name, string resourceGroup = null)
        {
            if (resourceGroup == null)
            {
                resourceGroup = this.config.Global.ResourceGroup;
            }

            this.client.IotHubResource.CreateEventHubConsumerGroup(resourceGroup, iotHubName, endpointName, name);
        }
    }
}