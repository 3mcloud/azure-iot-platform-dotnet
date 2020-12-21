// <copyright file="Devices.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.AppConfiguration;
using Mmm.Iot.Common.Services.External.AsaManager;
using Mmm.Iot.Common.Services.External.CosmosDb;
using Mmm.Iot.Common.Services.Helpers;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.IoTHubManager.Services.Extensions;
using Mmm.Iot.IoTHubManager.Services.Helpers;
using Mmm.Iot.IoTHubManager.Services.Models;
using Mmm.Iot.StorageAdapter.Services.Models;
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
        private const string DeviceConnectionStateCountQueryPrefix = "SELECT COUNT() AS numberOfDevices, connectionState FROM devices";
        private const string DeviceConnectionState = "connectionState";
        private const string DevicesConnectedQuery = "connectionState = 'Connected'";
        private const string TwinChangeDatabase = "iot";
        private const string AppConfigTenantInfoKey = "tenant";
        private const string AppConfigTwinChangeCollectionKey = "twin-change-collection";
        private readonly ITenantConnectionHelper tenantConnectionHelper;
        private readonly IAsaManagerClient asaManager;
        private readonly IDeviceQueryCache deviceQueryCache;
        private readonly IStorageClient storageClient;
        private readonly IAppConfigurationClient appConfigurationClient;

        public Devices(
            AppConfig config,
            ITenantConnectionHelper tenantConnectionHelper,
            IAsaManagerClient asaManagerClient,
            IDeviceQueryCache deviceQueryCache,
            IStorageClient storageClient,
            IAppConfigurationClient appConfigurationClient)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            this.tenantConnectionHelper = tenantConnectionHelper;
            this.asaManager = asaManagerClient;
            this.deviceQueryCache = deviceQueryCache;
            this.storageClient = storageClient;
            this.appConfigurationClient = appConfigurationClient;
        }

        public Devices(
            ITenantConnectionHelper tenantConnectionHelper,
            string ioTHubHostName,
            IAsaManagerClient asaManagerClient,
            IDeviceQueryCache deviceQueryCache,
            IStorageClient storageClient)
        {
            this.tenantConnectionHelper = tenantConnectionHelper ?? throw new ArgumentNullException("tenantConnectionHelper " + ioTHubHostName);
            this.asaManager = asaManagerClient;
            this.deviceQueryCache = deviceQueryCache;
            this.storageClient = storageClient;
        }

        public virtual string DocumentDataType
        {
            get
            {
                return "pcs";
            }
        }

        public virtual string DocumentDatabaseSuffix
        {
            get
            {
                return "storage";
            }
        }

        public virtual string DocumentDbDatabaseId
        {
            get
            {
                return $"{this.DocumentDataType}-{this.DocumentDatabaseSuffix}";
            }
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

        public async Task<DeviceServiceListModel> GetListAsync(string inputQuery, string continuationToken)
        {
            string querytoBeCached = inputQuery;
            IEnumerable<QueryConditionClause> clauses = null;
            IEnumerable<QueryConditionClause> deviceIdClauses = null;
            if (!string.IsNullOrWhiteSpace(inputQuery))
            {
                clauses = JsonConvert.DeserializeObject<IEnumerable<QueryConditionClause>>(inputQuery);
                deviceIdClauses = clauses.Where(x => x.Key == "deviceId" && x.Operator == "LK").ToList();

                if (deviceIdClauses != null && deviceIdClauses.Count() > 0)
                {
                    clauses = clauses.Where(x => x.Key != "deviceId" && x.Operator == "LK");
                    inputQuery = JsonConvert.SerializeObject(clauses);
                }

                if (!string.IsNullOrWhiteSpace(inputQuery))
                {
                    // Try to translate clauses to query
                    inputQuery = QueryConditionTranslator.ToQueryString(inputQuery);
                }
            }

            string tenantId = this.tenantConnectionHelper.TenantId;

            var resultModel = await this.deviceQueryCache.GetCachedQueryResultAsync(tenantId, querytoBeCached);

            if (resultModel != null)
            {
                return resultModel;
            }

            string query = string.Empty;
            int iotHublimit = 500;
            string deviceListValue = string.Empty;
            ResultWithContinuationToken<List<Twin>> allTwins = new ResultWithContinuationToken<List<Twin>>(new List<Twin>(), continuationToken);
            if (deviceIdClauses != null && deviceIdClauses.Count() > 0)
            {
                foreach (var deviceIdClause in deviceIdClauses)
                {
                    List<string> deviceIds = await this.GetDevicesBasedOnInputDeviceString(deviceIdClause.Value.ToString().ToLower(), tenantId);
                    for (int i = 0; i < (deviceIds.Count / iotHublimit) + 1; i++)
                    {
                        if (i != 0 && (deviceIds.Count % (i * iotHublimit)) <= 0)
                        {
                            break;
                        }

                        List<string> batchDeviceIds = deviceIds.Skip(i * iotHublimit).Take(iotHublimit).ToList();
                        if (batchDeviceIds != null && batchDeviceIds.Count > 0)
                        {
                            // deviceListValue = $"({string.Join(" or ", deviceIds.Select(v => $"deviceId = '{v}'"))})";
                            deviceListValue = string.Join("','", batchDeviceIds.Select(p => p));
                        }

                        if (!string.IsNullOrWhiteSpace(inputQuery))
                        {
                            // Try to translate clauses to query
                            query = $"{inputQuery} AND deviceId IN [{deviceListValue}]";
                        }
                        else
                        {
                            query = $" deviceId IN ['{deviceListValue}']";
                        }

                        int countOfDevicestoFetch = string.IsNullOrWhiteSpace(deviceListValue) ? MaximumGetList : deviceIds.Count();

                        var twins = await this.GetTwinByQueryAsync(
                            QueryPrefix,
                            query,
                            continuationToken,
                            countOfDevicestoFetch);

                        allTwins.Result.AddRange(twins.Result.Except(allTwins.Result));
                        while (!string.IsNullOrWhiteSpace(twins.ContinuationToken))
                        {
                            twins = await this.GetTwinByQueryAsync(
                            QueryPrefix,
                            query,
                            continuationToken,
                            countOfDevicestoFetch);
                            allTwins.Result.AddRange(twins.Result);
                        }
                    }
                }
            }
            else
            {
                allTwins = await this.GetTwinByQueryAsync(
                QueryPrefix,
                inputQuery,
                continuationToken,
                MaximumGetList);
            }

            var connectedEdgeDevices = await this.GetConnectedEdgeDevices(allTwins.Result);

            resultModel = new DeviceServiceListModel(
                allTwins.Result.Select(azureTwin => new DeviceServiceModel(
                    azureTwin,
                    this.tenantConnectionHelper.GetIotHubName(),
                    connectedEdgeDevices.ContainsKey(azureTwin.DeviceId))),
                allTwins.ContinuationToken);
            this.deviceQueryCache.SetTenantQueryResult(
                this.tenantConnectionHelper.TenantId,
                querytoBeCached,
                new DeviceQueryCacheResultServiceModel
                {
                    Result = resultModel,
                    ResultTimestamp = DateTimeOffset.Now,
                });

            return resultModel;
        }

        public async Task<DeviceServiceListModel> GetDeviceListAsync(string query, string continuationToken)
        {
            if (!string.IsNullOrWhiteSpace(query))
            {
                // Try to translate clauses to query
                query = QueryConditionTranslator.ToQueryString(query);
            }

            var twins = await this.GetTwinByQueryAsync(
                QueryPrefix,
                query,
                continuationToken,
                MaximumGetList);

            var connectedEdgeDevices = await this.GetConnectedEdgeDevices(twins.Result);

            var resultModel = new DeviceServiceListModel(
                twins.Result.Select(azureTwin => new DeviceServiceModel(
                    azureTwin,
                    this.tenantConnectionHelper.GetIotHubName(),
                    connectedEdgeDevices.ContainsKey(azureTwin.DeviceId))),
                twins.ContinuationToken);

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

        public async Task<TwinServiceListModel> GetDeploymentHistoryAsync(string deviceId, string tenantId)
        {
            var sql = QueryBuilder.GetDeviceDocumentsSqlByKey("Key", deviceId);

            FeedOptions queryOptions = new FeedOptions
            {
                EnableCrossPartitionQuery = true,
                EnableScanInQuery = true,
            };
            List<Document> docs = await this.storageClient.QueryDocumentsAsync(
                this.DocumentDbDatabaseId,
                $"{this.DocumentDataType}-{tenantId}",
                queryOptions,
                sql,
                0,
                1000);

            var result = docs == null ?
                new List<TwinServiceModel>() :
                docs
                    .Select(doc => new ValueServiceModel(doc)).Select(x => JsonConvert.DeserializeObject<TwinServiceModel>(x.Data))
                    .ToList();
            return new TwinServiceListModel(result, null);
        }

        public async Task<DeviceStatisticsServiceModel> GetDeviceStatisticsAsync(string query)
        {
            if (!string.IsNullOrWhiteSpace(query))
            {
                // Try to translate clauses to query
                query = QueryConditionTranslator.ToQueryString(query);
            }

            var data = await this.GetIotDataQueryAsync<DeviceConnectionStatusCountModel>(
                DeviceConnectionStateCountQueryPrefix,
                query,
                DeviceConnectionState,
                null,
                MaximumGetList); // Currently data does not show correct edge device connected status count. Will be supported in future.

            return new DeviceStatisticsServiceModel(data.Result);
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

        private async Task<List<string>> GetDevicesBasedOnInputDeviceString(string deviceInput, string tenantId)
        {
            var sql = QueryBuilder.GetDeviceDocumentsSqlByKeyLikeSearch("deviceId", deviceInput);

            var twinChangeResult = await this.storageClient.QueryDocumentsAsync(
                TwinChangeDatabase,
                this.GetTwinChangeCollectionId(tenantId),
                new FeedOptions
                {
                    EnableCrossPartitionQuery = true,
                },
                sql,
                0,
                10000);
            return twinChangeResult.Select(x => x.Id).ToList();
        }

        private string GetTwinChangeCollectionId(string tenantId)
        {
            return this.appConfigurationClient.GetValue(
                $"{AppConfigTenantInfoKey}:{tenantId}:{AppConfigTwinChangeCollectionKey}");
        }

        private async Task<ResultWithContinuationToken<List<T>>> GetIotDataQueryAsync<T>(
            string queryPrefix,
            string query,
            string groupBy,
            string continuationToken,
            int numberOfResult)
        {
            query = string.IsNullOrEmpty(query) ? queryPrefix : $"{queryPrefix} where {query}";

            query = string.IsNullOrEmpty(groupBy) ? query : $"{query} GROUP BY {groupBy}";

            var jsonResult = new List<string>();

            var jsonQuery = this.tenantConnectionHelper.GetRegistry().CreateQuery(query);

            QueryOptions options = new QueryOptions();
            options.ContinuationToken = continuationToken;

            while (jsonQuery.HasMoreResults && jsonResult.Count < numberOfResult)
            {
                var response = await jsonQuery.GetNextAsJsonAsync(options);
                options.ContinuationToken = response.ContinuationToken;
                jsonResult.AddRange(response);
            }

            List<T> result = jsonResult.Select(result => JsonConvert.DeserializeObject<T>(result)).ToList();

            return new ResultWithContinuationToken<List<T>>(result, options.ContinuationToken);
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