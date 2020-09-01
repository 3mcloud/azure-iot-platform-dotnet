// <copyright file="DpsManagementClient.cs" company="3M">
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
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Rest.Azure;
using Microsoft.Rest.ClientRuntime;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.TableStorage;
using Mmm.Iot.Common.Services.Models;

namespace Mmm.Iot.Common.Services.External.Azure
{
    public class DpsManagementClient : IDpsManagementClient
    {
        private readonly ResourceManagementClient client;
        private readonly AppConfig config;

        public DpsManagementClient(ResourceManagementClient client, AppConfig config)
        {
            this.client = client;
            this.config = config;
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

        public async Task DeleteAsync(string dpsName, string resourceGroup = null)
        {
            if (resourceGroup == null)
            {
                resourceGroup = this.config.Global.ResourceGroup;
            }

            try
            {
                if (!await this.Exists(dpsName, resourceGroup))
                {
                    throw new ResourceNotFoundException($"DPS {dpsName} does not exist!");
                }

                await this.client.Resources.BeginDeleteByIdAsync(
                    $"/subscriptions/{this.config.Global.SubscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Devices/ProvisioningServices/{dpsName}",
                    "2020-03-01");
            }
            catch (CloudException e)
            {
                if (e.Body.Code == "402002")
                {
                    throw new ResourceNotFoundException($"DPS {dpsName} does not exist or may be in process of deletion!");
                }
                else
                {
                    throw e;
                }
            }
        }

        public async Task<bool> Exists(string dpsName, string resourceGroup = null)
        {
            if (resourceGroup == null)
            {
                resourceGroup = this.config.Global.ResourceGroup;
            }

            try
            {
                var resource = await this.client.Resources.GetByIdAsync(
                    $"/subscriptions/{this.config.Global.SubscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Devices/ProvisioningServices/{dpsName}",
                    "2020-03-01");
            }
            catch (CloudException e)
            {
                if (e.Body.Code == "ResourceNotFound" || e.Body.Code == "402002")
                {
                    return false;
                }

                throw e;
            }

            return true;
        }
    }
}