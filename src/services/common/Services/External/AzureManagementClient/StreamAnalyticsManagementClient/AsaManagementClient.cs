// <copyright file="AsaManagementClient.cs" company="3M">
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
using Microsoft.Azure.Management.StreamAnalytics;
using Microsoft.Azure.Management.StreamAnalytics.Models;
using Microsoft.Rest.Azure;
using Microsoft.Rest.ClientRuntime;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.TableStorage;
using Mmm.Iot.Common.Services.Models;

namespace Mmm.Iot.Common.Services.External.Azure
{
    public class AsaManagementClient : IAsaManagementClient
    {
        private readonly StreamAnalyticsManagementClient client;
        private readonly AppConfig config;

        public AsaManagementClient(StreamAnalyticsManagementClient client, AppConfig config)
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

        public async Task DeleteAsync(string jobName, string resourceGroup = null, CancellationToken token = default(CancellationToken))
        {
            if (resourceGroup == null)
            {
                resourceGroup = this.config.Global.ResourceGroup;
            }

            try
            {
                await this.client.StreamingJobs.BeginDeleteAsync(resourceGroup, jobName, token);
            }
            catch (CloudException e)
            {
                if (e.Body.Code == "402002")
                {
                    throw new ResourceNotFoundException($"ASA job {jobName} does not exist or may be in process of deletion!");
                }
                else
                {
                    throw e;
                }
            }
        }

        public async Task<StreamingJob> RetrieveAsync(string jobName, string resourceGroup = null, CancellationToken token = default(CancellationToken))
        {
            if (resourceGroup == null)
            {
                resourceGroup = this.config.Global.ResourceGroup;
            }

            try
            {
                return await this.client.StreamingJobs.GetAsync(resourceGroup, jobName, null, token);
            }
            catch (CloudException e)
            {
                if (e.Body.Code == "ResourceNotFound")
                {
                    throw new ResourceNotFoundException("No such job");
                }

                throw e;
            }
        }

        public async Task StopAsync(string jobName, string resourceGroup = null, CancellationToken token = default(CancellationToken))
        {
            if (resourceGroup == null)
            {
                resourceGroup = this.config.Global.ResourceGroup;
            }

            try
            {
                await this.client.StreamingJobs.BeginStopAsync(resourceGroup, jobName, token);
            }
            catch (CloudException e)
            {
                if (e.Body.Code == "ResourceNotFound")
                {
                    throw new ResourceNotFoundException("No such job");
                }

                throw e;
            }
        }
    }
}