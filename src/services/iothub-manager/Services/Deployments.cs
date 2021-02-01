// <copyright file="Deployments.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.AppConfiguration;
using Mmm.Iot.Common.Services.External.CosmosDb;
using Mmm.Iot.Common.Services.External.StorageAdapter;
using Mmm.Iot.Common.Services.Helpers;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.Config.Services.Models;
using Mmm.Iot.IoTHubManager.Services.External;
using Mmm.Iot.IoTHubManager.Services.Helpers;
using Mmm.Iot.IoTHubManager.Services.Models;
using Mmm.Iot.StorageAdapter.Services.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Mmm.Iot.Config.Services.Models.DeviceStatusQueries;

namespace Mmm.Iot.IoTHubManager.Services
{
    public class Deployments : IDeployments
    {
        private const int MaxDeployments = 1000;
        private const string DeploymentNameLabel = "Name";
        private const string DeploymentGroupIdLabel = "DeviceGroupId";
        private const string DeploymentGroupNameLabel = "DeviceGroupName";
        private const string DeploymentPackageNameLabel = "PackageName";
        private const string RmCreatedLabel = "RMDeployment";
        private const string DeviceGroupIdParameter = "deviceGroupId";
        private const string DeviceGroupQueryParameter = "deviceGroupQuery";
        private const string NameParameter = "name";
        private const string PackageContentParameter = "packageContent";
        private const string ConfigurationTypeParameter = "configType";
        private const string PriorityParameter = "priority";
        private const string DeviceIdKey = "deviceId";
        private const string EdgeManifestSchema = "schemaVersion";
        private const string FailedQueryName = "error";
        private const string SuccessQueryName = "current";
        private const string DeploymentsCollection = "deployments";
        private const string DeploymentDevicePropertiesCollection = "deploymentdevices-{0}";
        private const string DeploymentEdgeModulePropertiesCollection = "deploymentedgemodules-{0}";
        private const string DeploymentModuleHistoryPropertiesCollection = "deploymentModulesHistory-{0}_{1}";
        private const string DeleteTag = "reserved.isDeleted";
        private const string LatestTag = "reserved.latest";
        private const string InActiveTag = "reserved.inactive";
        private const string ReactivatedTag = "reserved.reactivated";
        private const string AppConfigTenantInfoKey = "tenant";
        private const string AppConfigPcsCollectionKey = "pcs-collection";
        private readonly ILogger logger;
        private readonly IDeploymentEventLog deploymentLog;
        private readonly ITenantConnectionHelper tenantHelper;
        private readonly IConfigClient configClient;
        private readonly IStorageAdapterClient client;
        private readonly IDevices devices;
        private readonly AppConfig config;
        private readonly IStorageClient storageClient;
        private readonly IAppConfigurationClient appConfigurationClient;

        public Deployments(
            AppConfig config,
            ILogger<Deployments> logger,
            IDeploymentEventLog deploymentLog,
            ITenantConnectionHelper tenantConnectionHelper,
            IConfigClient packagesConfigClient,
            IStorageAdapterClient client,
            IDevices devices,
            IStorageClient storageClient,
            IAppConfigurationClient appConfigurationClient)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            this.tenantHelper = tenantConnectionHelper;
            this.deploymentLog = deploymentLog;
            this.logger = logger;
            this.configClient = packagesConfigClient;
            this.client = client;
            this.devices = devices;
            this.config = config;
            this.storageClient = storageClient;
            this.appConfigurationClient = appConfigurationClient;
        }

        public Deployments(ITenantConnectionHelper tenantHelper)
        {
            this.tenantHelper = tenantHelper ?? throw new ArgumentNullException("tenantHelper");
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

        public async Task<DeploymentServiceModel> CreateAsync(DeploymentServiceModel model, string userId, string tenantId)
        {
            if (string.IsNullOrEmpty(model.DeviceGroupId))
            {
                throw new ArgumentNullException(DeviceGroupIdParameter);
            }

            if (string.IsNullOrEmpty(model.DeviceGroupQuery) && (model.DeviceIds == null || (model.DeviceIds != null && model.DeviceIds.Count() == 0)))
            {
                throw new ArgumentNullException(DeviceGroupQueryParameter);
            }

            if (string.IsNullOrEmpty(model.Name))
            {
                throw new ArgumentNullException(NameParameter);
            }

            if (string.IsNullOrEmpty(model.PackageContent))
            {
                throw new ArgumentNullException(PackageContentParameter);
            }

            if (model.PackageType.Equals(PackageType.DeviceConfiguration)
                && string.IsNullOrEmpty(model.ConfigType))
            {
                throw new ArgumentNullException(ConfigurationTypeParameter);
            }

            if (model.Priority < 0)
            {
                throw new ArgumentOutOfRangeException(
                    PriorityParameter,
                    model.Priority,
                    "The priority provided should be 0 or greater");
            }

            var configuration = ConfigurationsHelper.ToHubConfiguration(model);

            // Update the Metrics related to previous deployment which targets the same device group as the metrics
            // will be overriden once the new deployment gets applied to the devices.
            var deviceMetrics = await this.UpdateMetricsOfCurrentDeployment(model.DeviceGroupId, model.Priority, tenantId);
            bool shouldMarkAsLatest = deviceMetrics.ShouldMarkAsLatest;

            var deploymentResult = await this.tenantHelper.GetRegistry().AddConfigurationAsync(configuration);

            // TODO: Add specific exception handling when exception types are exposed
            // https://github.com/Azure/azure-iot-sdk-csharp/issues/649
            var result = new DeploymentServiceModel(deploymentResult);

            // Setting the id so that deployment id is populated
            model.Id = result.Id;
            model.CreatedDateTimeUtc = result.CreatedDateTimeUtc;

            // Add latest tag to deployment if deployment has highest priority for the device group.
            if (shouldMarkAsLatest)
            {
                if (model.Tags == null)
                {
                    model.Tags = new List<string>();
                }

                model.Tags.Add(LatestTag);
            }
            else
            {
                // Update the Device Statuses for the DeploymentId for future references.
                model.DeploymentMetrics = result.DeploymentMetrics;
            }

            // Store the deployment details in Cosmos DB
            await this.StoreDeploymentInSecondaryStorage(model, userId);

            // Log a custom event to Application Insights
            // this.deploymentLog.LogDeploymentCreate(model, tenantId, userId);
            return model;
        }

        public async Task<DeploymentServiceListModel> ListAsync()
        {
            // TODO: Currently they only support 100 deployments
            var deployments = await this.tenantHelper.GetRegistry().GetConfigurationsAsync(MaxDeployments);

            if (deployments == null)
            {
                throw new ResourceNotFoundException($"No deployments found for {this.tenantHelper.GetIotHubName()} hub.");
            }

            List<DeploymentServiceModel> serviceModelDeployments =
                deployments.Where(this.CheckIfDeploymentWasMadeByRM)
                           .Select(config => new DeploymentServiceModel(config))
                           .OrderByDescending(conf => conf.CreatedDateTimeUtc)
                           .ToList();

            return new DeploymentServiceListModel(serviceModelDeployments);
        }

        public async Task<DeploymentServiceListModel> ListFromStorageAsync()
        {
            var deploymentsFromStorage = await this.GetListAsync();

            if (deploymentsFromStorage != null && deploymentsFromStorage.Items?.Count > 0)
            {
                deploymentsFromStorage.Items = deploymentsFromStorage.Items.Where(x => !x.Tags.Contains(DeleteTag) && !x.Tags.Contains(ReactivatedTag)).ToList();
                var deploymentsFromHub = await this.ListAsync();

                if (deploymentsFromHub != null && deploymentsFromHub.Items?.Count > 0)
                {
                    deploymentsFromStorage.Items.ForEach(x =>
                    {
                        if (x.Tags.Contains(LatestTag))
                        {
                            var deploymentFromHub = deploymentsFromHub.Items.FirstOrDefault(i => i.Id == x.Id);
                            if (deploymentFromHub != null)
                            {
                                x.DeploymentMetrics = deploymentFromHub.DeploymentMetrics;
                            }
                        }
                    });
                }
            }

            return deploymentsFromStorage;
        }

        public async Task<DeploymentServiceModel> GetAsync(string deploymentId, bool includeDeviceStatus = false, bool isLatest = true)
        {
            if (string.IsNullOrEmpty(deploymentId))
            {
                throw new ArgumentNullException(nameof(deploymentId));
            }

            DeploymentServiceModel deployment = await this.GetDeploymentFromStorageAsync(deploymentId);
            isLatest = deployment.Tags.Contains(LatestTag);
            if (isLatest)
            {
                var deploymentFromHub = await this.tenantHelper.GetRegistry().GetConfigurationAsync(deploymentId);

                if (deploymentFromHub == null)
                {
                    throw new ResourceNotFoundException($"Deployment with id {deploymentId} not found.");
                }

                if (!this.CheckIfDeploymentWasMadeByRM(deploymentFromHub))
                {
                    throw new ResourceNotSupportedException($"Deployment with id {deploymentId}" + @" was
                                                        created externally and therefore not supported");
                }

                IDictionary<string, DeploymentStatus> deviceStatuses = this.GetDeviceStatuses(deploymentFromHub);

                return new DeploymentServiceModel(deploymentFromHub)
                {
                    DeploymentMetrics =
                {
                    DeviceMetrics = this.CalculateDeviceMetrics(deviceStatuses),
                    DeviceStatuses = includeDeviceStatus ? deviceStatuses : null,
                },
                    Tags = new List<string>() { LatestTag },
                };
            }
            else
            {
                if (deployment != null && deployment.DeploymentMetrics != null)
                {
                    deployment.PackageContent = null;
                    if (deployment.DeploymentMetrics.DeviceStatuses == null)
                    {
                        deployment.DeploymentMetrics.DeviceStatuses = new Dictionary<string, DeploymentStatus>();
                    }

                    deployment.DeploymentMetrics.DeviceMetrics = this.CalculateDeviceMetrics(deployment.DeploymentMetrics.DeviceStatuses);
                    deployment.DeploymentMetrics.DeviceStatuses = includeDeviceStatus ? deployment.DeploymentMetrics.DeviceStatuses : null;
                }

                return deployment;
            }
        }

        public async Task DeleteAsync(string deploymentId, string userId, string tenantId, bool isDelete)
        {
            if (string.IsNullOrEmpty(deploymentId))
            {
                throw new ArgumentNullException(nameof(deploymentId));
            }

            var existingDeployment = await this.GetDeploymentFromStorageAsync(deploymentId);
            if (!existingDeployment.Tags.Contains(InActiveTag))
            {
                if (existingDeployment != null && existingDeployment.Tags.Contains(LatestTag))
                {
                    var currentDeployment = await this.GetAsync(deploymentId, true, true);

                    if (currentDeployment != null)
                    {
                        existingDeployment.DeploymentMetrics = currentDeployment.DeploymentMetrics;
                    }
                }

                await this.tenantHelper.GetRegistry().RemoveConfigurationAsync(deploymentId);

                // Mark the Deployment as Inactive in CosmosDb collection
                await this.MarkDeploymentAsInactive(existingDeployment, userId, isDelete, tenantId);
            }
            else
            {
                if (isDelete)
                {
                    await this.MarkDeploymentAsDeleted(existingDeployment, userId);
                }
            }

            // Log a custom event to Application Insights
            this.deploymentLog.LogDeploymentDelete(deploymentId, tenantId, userId);
        }

        public async Task<PackageApiModel> GetPackageAsync(string packageId)
        {
            return await this.configClient.GetPackageAsync(packageId);
        }

        public async Task<DeviceGroup> GetDeviceGroupAsync(string deviceGroupId)
        {
            return await this.configClient.GetDeviceGroupAsync(deviceGroupId);
        }

        public async Task ReactivateDeploymentAsyc(string deploymentId, string userId, string tenantId)
        {
            var deploymentFromStorage = await this.GetDeploymentFromStorageAsync(deploymentId);
            if (deploymentFromStorage == null)
            {
                throw new ResourceNotFoundException($"No Deployment details found with Id {deploymentId}");
            }

            if (!string.IsNullOrWhiteSpace(deploymentFromStorage.PackageId) && string.IsNullOrWhiteSpace(deploymentFromStorage.PackageContent))
            {
                var package = await this.GetPackageAsync(deploymentFromStorage.PackageId);
                if (package != null)
                {
                    deploymentFromStorage.PackageContent = package.Content;
                }
            }

            if (!string.IsNullOrWhiteSpace(deploymentFromStorage.DeviceGroupId) && string.IsNullOrWhiteSpace(deploymentFromStorage.DeviceGroupQuery))
            {
                var deviceGroup = await this.GetDeviceGroupAsync(deploymentFromStorage.DeviceGroupId);
                if (deviceGroup != null)
                {
                    deploymentFromStorage.DeviceGroupQuery = JsonConvert.SerializeObject(deviceGroup.Conditions);
                }
            }

            // clearing out all the existing tags as we are creating new deployment with the old model (inactivated) deployment properties
            deploymentFromStorage.Tags = new List<string>();

            await this.CreateAsync(deploymentFromStorage, userId, tenantId);

            await this.MarkDeploymentAsActive(deploymentId, userId);
        }

        public async Task<DeviceServiceListModel> GetDeviceListAsync(string deploymentId, List<string> deviceIds, string tenantId)
        {
            string query = string.Empty;
            int iotHublimit = 500;
            string deviceListValue = string.Empty;
            DeviceServiceListModel allDevices = new DeviceServiceListModel(new List<DeviceServiceModel>(), null);

            if (deviceIds.Count > 0)
            {
                for (int i = 0; i < (deviceIds.Count / iotHublimit) + 1; i++)
                {
                    if (i != 0 && (deviceIds.Count % (i * iotHublimit)) <= 0)
                    {
                        break;
                    }

                    List<string> batchDeviceIds = deviceIds.Skip(i * iotHublimit).Take(iotHublimit).ToList();
                    if (batchDeviceIds != null && batchDeviceIds.Count > 0)
                    {
                        deviceListValue = string.Join(",", batchDeviceIds.Select(p => $"'{p}'"));
                    }

                    query = $" deviceId IN [{deviceListValue}]";

                    int countOfDevicestoFetch = string.IsNullOrWhiteSpace(deviceListValue) ? 1000 : deviceIds.Count();

                    var devices = await this.devices.GetListAsync(query, null);

                    allDevices.Items.AddRange(devices.Items);

                    while (!string.IsNullOrWhiteSpace(devices.ContinuationToken))
                    {
                        devices = await this.devices.GetListAsync(query, null);
                        allDevices.Items.AddRange(devices.Items);
                    }
                }

                var deploymentDeviceHistory = await this.GetDeploymentDevicesAsync(deploymentId, tenantId);
                if (deploymentDeviceHistory != null && deploymentDeviceHistory.Items.Count > 0)
                {
                    allDevices.Items.ForEach(item =>
                    {
                        var twin = deploymentDeviceHistory.Items.FirstOrDefault(i => i.DeviceId == item.Id)?.Twin;

                        if (twin != null)
                        {
                            item.Twin = twin;
                        }

                        item.PreviousTwin = deploymentDeviceHistory.Items.FirstOrDefault(i => i.DeviceId == item.Id)?.PreviousFirmwareTwin;
                    });
                }
            }

            return allDevices;
        }

        public async Task<TwinServiceListModel> GetModulesListAsync(string deploymentId, string query, bool isLatest)
        {
            TwinServiceListModel moduletwins = null;

            if (!isLatest)
            {
                moduletwins = await this.GetDeploymentImapactedModulesAsync(deploymentId);
            }
            else
            {
                moduletwins = await this.devices.GetModuleTwinsByQueryAsync(query, null);
            }

            return moduletwins;
        }

        public async Task<List<DeviceDeploymentStatusServiceModel>> GetDeploymentStatusReport(string id, string tenantId, bool isLatest = true)
        {
            List<DeviceDeploymentStatusServiceModel> deviceDeploymentStatuses = new List<DeviceDeploymentStatusServiceModel>();
            var deploymentDetails = await this.GetAsync(id, true, isLatest);

            if (deploymentDetails != null && deploymentDetails.DeploymentMetrics != null && deploymentDetails.DeploymentMetrics.DeviceStatuses != null && deploymentDetails.DeploymentMetrics.DeviceStatuses.Keys.Count > 0)
            {
                var deviceIds = deploymentDetails.DeploymentMetrics.DeviceStatuses.Keys.ToList();

                var devices = await this.GetDeviceListAsync(id, deviceIds, tenantId);

                foreach (var item in deploymentDetails.DeploymentMetrics.DeviceStatuses)
                {
                    var reportedProperties = devices.Items.First(x => x.Id == item.Key).Twin.ReportedProperties;
                    var previosReportedProperties = devices.Items.First(x => x.Id == item.Key)?.PreviousTwin?.ReportedProperties;

                    // var json = JToken.Parse(JsonConvert.SerializeObject(reportedProperties));
                    // var fieldsCollector = new JsonFieldsCollector(json);
                    // var fields = fieldsCollector.GetAllFields();
                    var fields = this.FieldsCollection(reportedProperties);
                    Dictionary<string, JValue> previousFields = null;

                    if (previosReportedProperties != null)
                    {
                        previousFields = this.FieldsCollection(previosReportedProperties);
                    }

                    deviceDeploymentStatuses.Add(new DeviceDeploymentStatusServiceModel(item.Key, item.Value, fields, previousFields));
                }
            }

            return deviceDeploymentStatuses;
        }

        public async Task<IEnumerable<ValueServiceModel>> GetDeploymentHistory(string collectionId, string tenantId)
        {
            var sql = QueryBuilder.GetDeviceDocumentsSqlByKeyLikeSearch("CollectionId", collectionId);

            List<Document> deploymentHistory = null;
            try
            {
                deploymentHistory = await this.storageClient.QueryDocumentsAsync(
                        "pcs-storage",
                        this.GetPcsCollectionId(tenantId),
                        new FeedOptions
                        {
                            EnableCrossPartitionQuery = true,
                        },
                        sql,
                        0,
                        1000);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Error occured while fetching Deployment History from {this.GetPcsCollectionId(tenantId)}");
            }

            var result = deploymentHistory == null ?
                new List<ValueServiceModel>() :
                deploymentHistory.Select(doc => new ValueServiceModel(doc));

            return result;
        }

        public async Task<Dictionary<string, string>> GetDeployments(string collectionName, string tenantId)
        {
            var sql = QueryBuilder.GetDocumentsByProperty("CollectionId", collectionName);

            List<Document> docs = null;
            try
            {
                docs = await this.storageClient.QueryDocumentsAsync(
                        "pcs-storage",
                        this.GetPcsCollectionId(tenantId),
                        new FeedOptions
                        {
                            EnableCrossPartitionQuery = true,
                        },
                        sql,
                        0,
                        1000);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Error occured while fetching Deployments from {this.GetPcsCollectionId(tenantId)}");
            }

            var deploymentsFromStorage = docs == null ?
                new List<ValueServiceModel>() :
                docs.Select(doc => new ValueServiceModel(doc));

            var deployments = deploymentsFromStorage.Select(d => this.CreateDeploymentServiceModel(d));

            return deployments.ToDictionary(dep => dep.Id, dep => dep.Name);
        }

        private async Task<DeploymentHistoryListModel> GetDeploymentDevicesAsync(string deploymentId, string tenantId)
        {
            var sql = QueryBuilder.GetDeploymentDeviceDocumentsSqlByKey("Key", deploymentId);
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
                10000);

            return docs == null
                 ? new DeploymentHistoryListModel(null)
                 : new DeploymentHistoryListModel(docs.Select(doc => new ValueServiceModel(doc))
                .GroupBy(x => x.CollectionId)
                .Select(y => y.ToList()
                              .FirstOrDefault())
                .Select(x => JsonConvert.DeserializeObject<DeploymentHistoryModel>(x.Data))
                    .ToList());
        }

        private async Task<TwinServiceListModel> GetDeploymentImapactedModulesAsync(string deploymentId)
        {
            List<TwinServiceModel> deploymentImpactedModules = new List<TwinServiceModel>();
            var response = await this.client.GetAllAsync(string.Format(DeploymentEdgeModulePropertiesCollection, deploymentId));

            if (response != null && response.Items.Count > 0)
            {
                deploymentImpactedModules.AddRange(response.Items.Select(this.CreateTwinServiceModel));
            }

            return new TwinServiceListModel(deploymentImpactedModules);
        }

        private bool CheckIfDeploymentWasMadeByRM(Configuration conf)
        {
            return conf.Labels != null &&
                   conf.Labels.ContainsKey(RmCreatedLabel) &&
                   bool.TryParse(conf.Labels[RmCreatedLabel], out var res) && res;
        }

        private IDictionary<string, DeploymentStatus> GetDeviceStatuses(Configuration deployment)
        {
            string deploymentType = null;
            if (ConfigurationsHelper.IsEdgeDeployment(deployment))
            {
                deploymentType = PackageType.EdgeManifest.ToString();
            }
            else
            {
                deploymentType = PackageType.DeviceConfiguration.ToString();
            }

            deployment.Labels.TryGetValue(ConfigurationsHelper.ConfigTypeLabel, out string configType);
            var queries = GetQueries(deploymentType, configType);

            string deploymentId = deployment.Id;
            var appliedDevices = this.GetDevicesInQuery(queries[QueryType.APPLIED], deploymentId);

            var deviceWithStatus = new Dictionary<string, DeploymentStatus>();

            if (!ConfigurationsHelper.IsEdgeDeployment(deployment) && !configType.Equals(ConfigType.Firmware.ToString()))
            {
                foreach (var devices in appliedDevices)
                {
                    deviceWithStatus.Add(devices, DeploymentStatus.Unknown);
                }

                return deviceWithStatus;
            }

            // Get reported status from custom Metrics if available otherwise use default queries
            var successfulDevices = this.GetDevicesInQuery(deployment.Metrics.Queries.ContainsKey(SuccessQueryName) ? deployment.Metrics.Queries[SuccessQueryName] : queries[QueryType.SUCCESSFUL], deploymentId);
            var failedDevices = this.GetDevicesInQuery(deployment.Metrics.Queries.ContainsKey(FailedQueryName) ? deployment.Metrics.Queries[FailedQueryName] : queries[QueryType.FAILED], deploymentId);

            foreach (var device in appliedDevices)
            {
                if (successfulDevices.Contains(device))
                {
                    deviceWithStatus.Add(device, DeploymentStatus.Succeeded);
                }
                else if (failedDevices.Contains(device))
                {
                    deviceWithStatus.Add(device, DeploymentStatus.Failed);
                }
                else
                {
                    deviceWithStatus.Add(device, DeploymentStatus.Pending);
                }
            }

            return deviceWithStatus;
        }

        private HashSet<string> GetDevicesInQuery(string hubQuery, string deploymentId)
        {
            var query = string.Format(hubQuery, deploymentId);
            var queryResponse = this.tenantHelper.GetRegistry().CreateQuery(query);
            var deviceIds = new HashSet<string>();

            try
            {
                while (queryResponse.HasMoreResults)
                {
                    // TODO: Add pagination with queryOptions
                    var resultSet = queryResponse.GetNextAsJsonAsync();
                    foreach (var result in resultSet.Result)
                    {
                        var deviceId = JToken.Parse(result)[DeviceIdKey];
                        deviceIds.Add(deviceId.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error getting status of devices in query {query}", query);
            }

            return deviceIds;
        }

        private IDictionary<DeploymentStatus, long> CalculateDeviceMetrics(
            IDictionary<string,
            DeploymentStatus> deviceStatuses)
        {
            if (deviceStatuses == null)
            {
                return null;
            }

            IDictionary<DeploymentStatus, long> deviceMetrics = new Dictionary<DeploymentStatus, long>();

            deviceMetrics[DeploymentStatus.Succeeded] = deviceStatuses.Where(item =>
                                                            item.Value == DeploymentStatus.Succeeded).LongCount();

            deviceMetrics[DeploymentStatus.Failed] = deviceStatuses.Where(item =>
                                                            item.Value == DeploymentStatus.Failed).LongCount();

            deviceMetrics[DeploymentStatus.Pending] = deviceStatuses.Where(item =>
                                                            item.Value == DeploymentStatus.Pending).LongCount();

            return deviceMetrics;
        }

        private async Task StoreDeploymentInSecondaryStorage(DeploymentServiceModel deployment, string userId)
        {
            if (string.IsNullOrWhiteSpace(deployment.ETag))
            {
                AuditHelper.AddAuditingData(deployment, userId);
            }
            else
            {
                AuditHelper.UpdateAuditingData(deployment, userId);
            }

            var value = JsonConvert.SerializeObject(
                                                    deployment,
                                                    Formatting.Indented,
                                                    new JsonSerializerSettings
                                                    {
                                                        NullValueHandling = NullValueHandling.Ignore,
                                                    });

            var response = await this.client.UpdateAsync(DeploymentsCollection, deployment.Id, value, deployment.ETag);
        }

        private async Task<(bool ShouldMarkAsLatest, string DeploymentId)> UpdateMetricsOfCurrentDeployment(string deviceGroupId, int priority, string tenantId)
        {
            var deploymentsFromHub = await this.ListAsync();

            var deploymentsOfDeviceGroup = deploymentsFromHub.Items.Where(i => i.DeviceGroupId == deviceGroupId).OrderByDescending(p => p.Priority).ThenByDescending(q => q.CreatedDateTimeUtc);

            string deploymentId = string.Empty;
            if (deploymentsOfDeviceGroup != null && deploymentsOfDeviceGroup.Count() > 0)
            {
                var deployment = deploymentsOfDeviceGroup.First();

                if (priority >= deployment.Priority)
                {
                    var getDeploymentFromHub = this.GetDeploymentAsync(deployment.Id);

                    var getDeploymentFromCOSMOS = this.GetDeploymentFromStorageAsync(deployment.Id);

                    await Task.WhenAll(getDeploymentFromHub, getDeploymentFromCOSMOS);

                    var deploymentDetails = await getDeploymentFromHub;

                    DeploymentServiceModel currentDeployment = await getDeploymentFromCOSMOS;

                    // Update the Device Statuses for the DeploymentId for future references.
                    currentDeployment.DeploymentMetrics = new DeploymentMetricsServiceModel(deploymentDetails.SystemMetrics, deploymentDetails.Metrics);

                    currentDeployment.DeploymentMetrics.DeviceStatuses = this.GetDeviceStatuses(deploymentDetails);

                    if (string.IsNullOrWhiteSpace(currentDeployment.TargetCondition))
                    {
                        currentDeployment.TargetCondition = deploymentDetails.TargetCondition;
                    }

                    // Since the deployment that will be created have highest priority, remove latest tag on current deployment
                    if (currentDeployment?.Tags != null)
                    {
                        var existingTag = currentDeployment.Tags.FirstOrDefault(t => t.Equals(LatestTag, StringComparison.OrdinalIgnoreCase));
                        if (existingTag != null)
                        {
                            currentDeployment.Tags.Remove(existingTag);
                        }
                    }

                    var value = JsonConvert.SerializeObject(
                        currentDeployment,
                        Formatting.Indented,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                        });

                    var updateDeployment = await this.client.UpdateAsync(DeploymentsCollection, currentDeployment.Id, value, currentDeployment.ETag);

                    return (true, currentDeployment.Id);
                }
                else
                {
                    return (false, deploymentId);
                }
            }

            // Since the deployment that will be created have highest priority, mark it as the latest
            return (true, deploymentId);
        }

        private string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        private async Task<Configuration> GetDeploymentAsync(string deploymentId)
        {
            if (string.IsNullOrEmpty(deploymentId))
            {
                throw new ArgumentNullException(nameof(deploymentId));
            }

            var deployment = await this.tenantHelper.GetRegistry().GetConfigurationAsync(deploymentId);

            if (deployment == null)
            {
                throw new ResourceNotFoundException($"Deployment with id {deploymentId} not found.");
            }

            if (!this.CheckIfDeploymentWasMadeByRM(deployment))
            {
                throw new ResourceNotSupportedException($"Deployment with id {deploymentId}" + @" was
                                                        created externally and therefore not supported");
            }

            return deployment;
        }

        private async Task<DeploymentServiceModel> GetDeploymentFromStorageAsync(string deploymentId)
        {
            var response = await this.client.GetAsync(DeploymentsCollection, deploymentId);
            return this.CreateDeploymentServiceModel(response);
        }

        private DeploymentServiceModel CreateDeploymentServiceModel(ValueApiModel response)
        {
            var output = JsonConvert.DeserializeObject<DeploymentServiceModel>(response.Data);
            output.Id = response.Key;
            output.ETag = response.ETag;
            if (output.Tags == null)
            {
                output.Tags = new List<string>();
            }

            return output;
        }

        private TwinServiceModel CreateTwinServiceModel(ValueApiModel response)
        {
            var output = JsonConvert.DeserializeObject<TwinServiceModel>(response.Data);
            output.ETag = response.ETag;
            return output;
        }

        private async Task<DeploymentServiceModel> MarkDeploymentAsInactive(DeploymentServiceModel existingDeployment, string userId, bool isDelete, string tenantId)
        {
            if (existingDeployment != null)
            {
                if (existingDeployment.Tags == null)
                {
                    existingDeployment.Tags = new List<string>();
                }

                if (existingDeployment.Tags.Contains(InActiveTag, StringComparer.InvariantCultureIgnoreCase))
                {
                    return existingDeployment;
                }

                existingDeployment.Tags.Add(InActiveTag);
                if (isDelete)
                {
                    existingDeployment.Tags.Add(DeleteTag);
                }

                bool isLatestDeployment = existingDeployment.Tags.Contains(LatestTag, StringComparer.InvariantCultureIgnoreCase);

                if (isLatestDeployment)
                {
                    existingDeployment.Tags.Remove(LatestTag);
                }

                AuditHelper.UpdateAuditingData(existingDeployment, userId);

                var value = JsonConvert.SerializeObject(
                    existingDeployment,
                    Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                    });
                var response = await this.client.UpdateAsync(DeploymentsCollection, existingDeployment.Id, value, existingDeployment.ETag);

                if (isLatestDeployment)
                {
                    // Mark the deployment with highest priority and which is created last as the latest deployment.
                    var deployments = await this.ListAsync();

                    if (deployments != null && deployments.Items.Count > 0)
                    {
                        var deploymentsOfDeviceGroup = deployments.Items.Where(i => i.DeviceGroupId == existingDeployment.DeviceGroupId).OrderByDescending(p => p.Priority).ThenByDescending(q => q.CreatedDateTimeUtc);

                        if (deploymentsOfDeviceGroup != null && deploymentsOfDeviceGroup.Count() > 0)
                        {
                            var latestDeployment = deploymentsOfDeviceGroup.First();

                            var latestDeploymentFromStorage = await this.GetDeploymentFromStorageAsync(latestDeployment.Id);

                            if (latestDeploymentFromStorage != null)
                            {
                                if (latestDeploymentFromStorage.Tags == null)
                                {
                                    latestDeploymentFromStorage.Tags = new List<string>();
                                }

                                if (!latestDeploymentFromStorage.Tags.Contains(LatestTag))
                                {
                                    latestDeploymentFromStorage.Tags.Add(LatestTag);

                                    var storageValue = JsonConvert.SerializeObject(
                                                                            latestDeploymentFromStorage,
                                                                            Formatting.Indented,
                                                                            new JsonSerializerSettings
                                                                            {
                                                                                NullValueHandling = NullValueHandling.Ignore,
                                                                            });
                                    await this.client.UpdateAsync(DeploymentsCollection, latestDeployment.Id, storageValue, latestDeploymentFromStorage.ETag);
                                }
                            }
                        }
                    }
                }
            }

            return existingDeployment;
        }

        private async Task<DeploymentServiceModel> MarkDeploymentAsActive(string deploymentId, string userId)
        {
            var existingDeployment = await this.GetDeploymentFromStorageAsync(deploymentId);

            if (existingDeployment?.Tags == null)
            {
                return existingDeployment;
            }

            var existingTag = existingDeployment.Tags.FirstOrDefault(t => t.Equals(InActiveTag, StringComparison.OrdinalIgnoreCase));
            if (existingTag == null)
            {
                return existingDeployment;
            }

            existingDeployment.Tags.Add(ReactivatedTag);

            AuditHelper.UpdateAuditingData(existingDeployment, userId);

            var value = JsonConvert.SerializeObject(
                existingDeployment,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                });
            var response = await this.client.UpdateAsync(DeploymentsCollection, deploymentId, value, existingDeployment.ETag);

            return existingDeployment;
        }

        private async Task<DeploymentServiceModel> MarkDeploymentAsDeleted(DeploymentServiceModel existingDeployment, string userId)
        {
            if (existingDeployment != null)
            {
                existingDeployment.Tags.Add(DeleteTag);

                AuditHelper.UpdateAuditingData(existingDeployment, userId);

                var value = JsonConvert.SerializeObject(
                    existingDeployment,
                    Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                    });
                var response = await this.client.UpdateAsync(DeploymentsCollection, existingDeployment.Id, value, existingDeployment.ETag);
            }

            return existingDeployment;
        }

        private async Task<DeploymentServiceListModel> GetListAsync()
        {
            IEnumerable<DeploymentServiceModel> deployments = null;
            var response = await this.client.GetAllAsync(DeploymentsCollection);

            if (response != null && response.Items.Count > 0)
            {
                deployments = response.Items.Select(this.CreateDeploymentServiceModel);
            }

            return new DeploymentServiceListModel(deployments?.OrderByDescending(x => x.CreatedDateTimeUtc).ToList());
        }

        private Dictionary<string, JValue> FieldsCollection(Dictionary<string, JToken> reportedProperties)
        {
            var json = JToken.Parse(JsonConvert.SerializeObject(reportedProperties));
            var fieldsCollector = new JsonFieldsCollector(json);
            return fieldsCollector.GetAllFields();
        }

        private string GetPcsCollectionId(string tenantId)
        {
            return this.appConfigurationClient.GetValue(
                $"{AppConfigTenantInfoKey}:{tenantId}:{AppConfigPcsCollectionKey}");
        }

        private DeploymentServiceModel CreateDeploymentServiceModel(ValueServiceModel response)
        {
            var output = JsonConvert.DeserializeObject<DeploymentServiceModel>(response.Data);
            output.Id = response.Key;
            output.ETag = response.ETag;
            if (output.Tags == null)
            {
                output.Tags = new List<string>();
            }

            return output;
        }
    }
}