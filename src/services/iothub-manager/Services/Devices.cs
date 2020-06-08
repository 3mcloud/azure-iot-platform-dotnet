// <copyright file="Devices.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.AsaManager;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.IoTHubManager.Services.Extensions;
using Mmm.Iot.IoTHubManager.Services.Helpers;
using Mmm.Iot.IoTHubManager.Services.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AuthenticationType = Mmm.Iot.IoTHubManager.Services.Models.AuthenticationType;

namespace Mmm.Iot.IoTHubManager.Services
{
    public delegate Task<DevicePropertyServiceModel> DevicePropertyDelegate(DevicePropertyServiceModel model);

    public class Devices : IDevices
    {
        private const int MaximumGetList = 1000;
        private const string QueryPrefix = "SELECT * FROM devices";
        private const string ModuleQueryPrefix = "SELECT * FROM devices.modules";
        private const string DevicesConnectedQuery = "connectionState = 'Connected'";
        private readonly ITenantConnectionHelper tenantConnectionHelper;
        private readonly IAsaManagerClient asaManager;
        private readonly IDeviceQueryCache deviceQueryCache;

        public Devices(
            AppConfig config,
            ITenantConnectionHelper tenantConnectionHelper,
            IAsaManagerClient asaManagerClient,
            IDeviceQueryCache deviceQueryCache)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            this.tenantConnectionHelper = tenantConnectionHelper;
            this.asaManager = asaManagerClient;
            this.deviceQueryCache = deviceQueryCache;
        }

        public Devices(
            ITenantConnectionHelper tenantConnectionHelper,
            string ioTHubHostName,
            IAsaManagerClient asaManagerClient,
            IDeviceQueryCache deviceQueryCache)
        {
            this.tenantConnectionHelper = tenantConnectionHelper ?? throw new ArgumentNullException("tenantConnectionHelper " + ioTHubHostName);
            this.asaManager = asaManagerClient;
            this.deviceQueryCache = deviceQueryCache;
        }

        // Ping the registry to see if the connection is healthy
        public async Task<StatusResultServiceModel> StatusAsync()
        {
            var result = new StatusResultServiceModel(false, string.Empty);
            try
            {
                await this.tenantConnectionHelper.GetRegistry().GetDeviceAsync("healthcheck");
                result.IsHealthy = true;
                result.Message = "Alive and Well!";
            }
            catch (Exception e)
            {
                result.Message = e.Message;
            }

            return result;
        }

        public async Task<DeviceServiceListModel> GetListAsync(string query, string continuationToken)
        {
            if (!string.IsNullOrWhiteSpace(query))
            {
                // Try to translate clauses to query
                query = QueryConditionTranslator.ToQueryString(query);
            }

            var resultModel = await this.deviceQueryCache.GetCachedQueryResultAsync(this.tenantConnectionHelper.TenantId, query);

            if (resultModel != null)
            {
                return resultModel;
            }

            var twins = await this.GetTwinByQueryAsync(
                QueryPrefix,
                query,
                continuationToken,
                MaximumGetList);

            var connectedEdgeDevices = await this.GetConnectedEdgeDevices(twins.Result);

            resultModel = new DeviceServiceListModel(
                twins.Result.Select(azureTwin => new DeviceServiceModel(
                    azureTwin,
                    this.tenantConnectionHelper.GetIotHubName(),
                    connectedEdgeDevices.ContainsKey(azureTwin.DeviceId))),
                twins.ContinuationToken);
            this.deviceQueryCache.SetTenantQueryResult(
                this.tenantConnectionHelper.TenantId,
                query,
                new DeviceQueryCacheResultServiceModel
                {
                    Result = resultModel,
                    ResultTimestamp = DateTimeOffset.Now,
                });

            return resultModel;
        }

        public async Task<DeviceTwinName> GetDeviceTwinNamesAsync()
        {
            var content = await this.GetListAsync(string.Empty, string.Empty);

            return content.GetDeviceTwinNames();
        }

        public async Task<DeviceServiceModel> GetAsync(string id)
        {
            var device = this.tenantConnectionHelper.GetRegistry().GetDeviceAsync(id);
            var twin = this.tenantConnectionHelper.GetRegistry().GetTwinAsync(id);

            await Task.WhenAll(device, twin);

            if (device.Result == null)
            {
                throw new ResourceNotFoundException("The device doesn't exist.");
            }

            var isEdgeConnectedDevice = await this.DoesDeviceHaveConnectedModules(device.Result.Id);

            return new DeviceServiceModel(device.Result, twin.Result, this.tenantConnectionHelper.GetIotHubName(), isEdgeConnectedDevice);
        }

        public async Task<DeviceServiceModel> CreateAsync(DeviceServiceModel device)
        {
            if (device.IsEdgeDevice &&
                device.Authentication != null &&
                !device.Authentication.AuthenticationType.Equals(AuthenticationType.Sas))
            {
                throw new InvalidInputException("Edge devices only support symmetric key authentication.");
            }

            // auto generate DeviceId, if missing
            if (string.IsNullOrEmpty(device.Id))
            {
                device.Id = Guid.NewGuid().ToString();
            }

            var azureDevice = await this.tenantConnectionHelper.GetRegistry().AddDeviceAsync(device.ToAzureModel());

            Twin azureTwin;
            if (device.Twin == null)
            {
                azureTwin = await this.tenantConnectionHelper.GetRegistry().GetTwinAsync(device.Id);
            }
            else
            {
                azureTwin = await this.tenantConnectionHelper.GetRegistry().UpdateTwinAsync(device.Id, device.Twin.ToAzureModel(), "*");
            }

            await this.asaManager.BeginDeviceGroupsConversionAsync();
            return new DeviceServiceModel(azureDevice, azureTwin, this.tenantConnectionHelper.GetIotHubName());
        }

        public async Task<DeviceServiceModel> UpdateAsync(DeviceServiceModel device, DevicePropertyDelegate devicePropertyDelegate)
        {
            // validate device module
            var azureDevice = await this.tenantConnectionHelper.GetRegistry().GetDeviceAsync(device.Id);
            if (azureDevice == null)
            {
                throw new ResourceNotFoundException($"Device {device.Id} could not be found on this tenant's IoT Hub. You must create the device first before calling the update method.");
            }

            Twin azureTwin;
            if (device.Twin == null)
            {
                azureTwin = await this.tenantConnectionHelper.GetRegistry().GetTwinAsync(device.Id);
            }
            else
            {
                azureTwin = await this.tenantConnectionHelper.GetRegistry().UpdateTwinAsync(device.Id, device.Twin.ToAzureModel(), device.Twin.ETag);

                // Update the deviceGroupFilter cache, no need to wait
                var model = new DevicePropertyServiceModel();

                if (JsonConvert.DeserializeObject(JsonConvert.SerializeObject(device.Twin.Tags)) is JToken tagRoot)
                {
                    model.Tags = new HashSet<string>(tagRoot.GetAllLeavesPath());
                }

                if (JsonConvert.DeserializeObject(JsonConvert.SerializeObject(device.Twin.ReportedProperties)) is JToken reportedRoot)
                {
                    model.Reported = new HashSet<string>(reportedRoot.GetAllLeavesPath());
                }

                _ = devicePropertyDelegate(model);
            }

            await this.asaManager.BeginDeviceGroupsConversionAsync();
            return new DeviceServiceModel(azureDevice, azureTwin, this.tenantConnectionHelper.GetIotHubName());
        }

        public async Task DeleteAsync(string id)
        {
            await this.tenantConnectionHelper.GetRegistry().RemoveDeviceAsync(id);
        }

        public async Task<TwinServiceModel> GetModuleTwinAsync(string deviceId, string moduleId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new InvalidInputException("A valid deviceId must be provided.");
            }

            if (string.IsNullOrWhiteSpace(moduleId))
            {
                throw new InvalidInputException("A valid moduleId must be provided.");
            }

            var twin = await this.tenantConnectionHelper.GetRegistry().GetTwinAsync(deviceId, moduleId);
            return new TwinServiceModel(twin);
        }

        public async Task<TwinServiceListModel> GetModuleTwinsByQueryAsync(
            string query,
            string continuationToken)
        {
            var twins = await this.GetTwinByQueryAsync(
                ModuleQueryPrefix,
                query,
                continuationToken,
                MaximumGetList);
            var result = twins.Result.Select(twin => new TwinServiceModel(twin)).ToList();

            return new TwinServiceListModel(result, twins.ContinuationToken);
        }

        private async Task<ResultWithContinuationToken<List<Twin>>> GetTwinByQueryAsync(
            string queryPrefix,
            string query,
            string continuationToken,
            int numberOfResult)
        {
            query = string.IsNullOrEmpty(query) ? queryPrefix : $"{queryPrefix} where {query}";

            var twins = new List<Twin>();

            var twinQuery = this.tenantConnectionHelper.GetRegistry().CreateQuery(query);

            QueryOptions options = new QueryOptions();
            options.ContinuationToken = continuationToken;

            while (twinQuery.HasMoreResults && twins.Count < numberOfResult)
            {
                var response = await twinQuery.GetNextAsTwinAsync(options);
                options.ContinuationToken = response.ContinuationToken;
                twins.AddRange(response);
            }

            return new ResultWithContinuationToken<List<Twin>>(twins, options.ContinuationToken);
        }

        private async Task<Dictionary<string, Twin>> GetConnectedEdgeDevices(List<Twin> twins)
        {
            var devicesWithConnectedModules = await this.GetDevicesWithConnectedModules();
            var edgeTwins = twins
                .Where(twin => twin.Capabilities?.IotEdge ?? twin.Capabilities?.IotEdge ?? false)
                .Where(edgeDvc => devicesWithConnectedModules.Contains(edgeDvc.DeviceId))
                .ToDictionary(edgeDevice => edgeDevice.DeviceId, edgeDevice => edgeDevice);
            return edgeTwins;
        }

        private async Task<HashSet<string>> GetDevicesWithConnectedModules()
        {
            var connectedEdgeDevices = new HashSet<string>();

            var edgeModules = await this.GetModuleTwinsByQueryAsync(DevicesConnectedQuery, string.Empty);
            foreach (var model in edgeModules.Items)
            {
                connectedEdgeDevices.Add(model.DeviceId);
            }

            return connectedEdgeDevices;
        }

        private async Task<bool> DoesDeviceHaveConnectedModules(string deviceId)
        {
            var query = $"deviceId='{deviceId}' AND {DevicesConnectedQuery}";
            var edgeModules = await this.GetModuleTwinsByQueryAsync(query, string.Empty);
            return edgeModules.Items.Any();
        }

        private class ResultWithContinuationToken<T>
        {
            public ResultWithContinuationToken(T queryResult, string continuationToken)
            {
                this.Result = queryResult;
                this.ContinuationToken = continuationToken;
            }

            public T Result { get; private set; }

            public string ContinuationToken { get; private set; }
        }
    }
}