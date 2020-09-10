// <copyright file="Deployments.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.StorageAdapter;
using Mmm.Iot.Common.Services.Helpers;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.Config.Services.Models;
using Mmm.Iot.IoTHubManager.Services.External;
using Mmm.Iot.IoTHubManager.Services.Helpers;
using Mmm.Iot.IoTHubManager.Services.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Mmm.Iot.Config.Services.Models.DeviceStatusQueries;

namespace Mmm.Iot.IoTHubManager.Services
{
    public class Deployments : IDeployments
    {
        private const int MaxDeployments = 100;
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
        private const string DeploymentHistoryPropertiesCollection = "deploymentHistory-{0}_{1}";
        private const string DeleteTag = "reserved.isDeleted";
        private const string LatestTag = "reserved.latest";
        private const string InActiveTag = "reserved.inactive";
        private const string ReactivatedTag = "reserved.reactivated";
        private readonly ILogger logger;
        private readonly IDeploymentEventLog deploymentLog;
        private readonly ITenantConnectionHelper tenantHelper;
        private readonly IConfigClient configClient;
        private readonly IStorageAdapterClient client;
        private readonly IDevices devices;

        public Deployments(
            AppConfig config,
            ILogger<Deployments> logger,
            IDeploymentEventLog deploymentLog,
            ITenantConnectionHelper tenantConnectionHelper,
            IConfigClient packagesConfigClient,
            IStorageAdapterClient client,
            IDevices devices)
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
        }

        public Deployments(ITenantConnectionHelper tenantHelper)
        {
            this.tenantHelper = tenantHelper ?? throw new ArgumentNullException("tenantHelper");
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
            bool shouldMarkAsLatest = await this.UpdateMetricsOfCurrentDeployment(model.DeviceGroupId, model.Priority);

            // TODO: Add specific exception handling when exception types are exposed
            // https://github.com/Azure/azure-iot-sdk-csharp/issues/649
            var result = new DeploymentServiceModel(await this.tenantHelper.GetRegistry().AddConfigurationAsync(configuration));

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

            if (isLatest)
            {
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

                IDictionary<string, DeploymentStatus> deviceStatuses = this.GetDeviceStatuses(deployment);

                return new DeploymentServiceModel(deployment)
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
                DeploymentServiceModel deployment = await this.GetDeploymentFromStorageAsync(deploymentId);

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
                List<TwinServiceModel> deviceTwins = null;
                List<TwinServiceModel> moduleTwins = null;
                if (existingDeployment != null && existingDeployment.Tags.Contains(LatestTag))
                {
                    var currentDeployment = await this.GetAsync(deploymentId, true, true);

                    if (currentDeployment != null)
                    {
                        existingDeployment.DeploymentMetrics = currentDeployment.DeploymentMetrics;
                    }

                    deviceTwins = await this.GetDeviceProperties(currentDeployment.DeploymentMetrics.DeviceStatuses.Keys);
                    if (currentDeployment.PackageType == PackageType.EdgeManifest)
                    {
                        moduleTwins = await this.GetDeviceProperties(currentDeployment.DeploymentMetrics.DeviceStatuses.Keys, PackageType.EdgeManifest);
                    }
                }

                await this.tenantHelper.GetRegistry().RemoveConfigurationAsync(deploymentId);

                // Mark the Deployment as Inactive in CosmosDb collection
                await this.MarkDeploymentAsInactive(existingDeployment, userId, deviceTwins, moduleTwins, isDelete);
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

        public async Task<DeviceServiceListModel> GetDeviceListAsync(string deploymentId, string query, bool isLatest)
        {
            var devices = await this.devices.GetListAsync(query, null);

            if (!isLatest)
            {
                var deviceTwinProperties = await this.GetDeploymentDevicesAsync(deploymentId);
                if (deviceTwinProperties != null && deviceTwinProperties.Items.Count > 0)
                {
                    devices.Items.ForEach(item =>
                    {
                        item.Twin = deviceTwinProperties.Items.LastOrDefault(i => i.DeviceId == item.Id);
                    });
                }
            }

            return devices;
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

        public async Task<List<DeviceDeploymentStatusServiceModel>> GetDeploymentStatusReport(string id, bool isLatest = true)
        {
            List<DeviceDeploymentStatusServiceModel> deviceDeploymentStatuses = new List<DeviceDeploymentStatusServiceModel>();
            var deploymentDetails = await this.GetAsync(id, true, isLatest);

            if (deploymentDetails != null && deploymentDetails.DeploymentMetrics != null && deploymentDetails.DeploymentMetrics.DeviceStatuses != null && deploymentDetails.DeploymentMetrics.DeviceStatuses.Keys.Count > 0)
            {
                string deviceQuery = @"deviceId IN [{0}]";
                var deviceIdsQuery = string.Join(",", deploymentDetails.DeploymentMetrics.DeviceStatuses.Keys.Select(d => $"'{d}'"));
                var query = string.Format(deviceQuery, deviceIdsQuery);

                DeviceServiceListModel devices = await this.GetDeviceListAsync(id, query, isLatest);

                foreach (var item in deploymentDetails.DeploymentMetrics.DeviceStatuses)
                {
                    var reportedProperties = devices.Items.First(x => x.Id == item.Key).Twin.ReportedProperties;

                    var json = JToken.Parse(JsonConvert.SerializeObject(reportedProperties));
                    var fieldsCollector = new JsonFieldsCollector(json);
                    var fields = fieldsCollector.GetAllFields();
                    deviceDeploymentStatuses.Add(new DeviceDeploymentStatusServiceModel(item.Key, item.Value, fields));
                }
            }

            return deviceDeploymentStatuses;
        }

        private async Task<TwinServiceListModel> GetDeploymentDevicesAsync(string deploymentId)
        {
            List<TwinServiceModel> deploymentImpactedDevices = new List<TwinServiceModel>();
            var response = await this.client.GetAllAsync(string.Format(DeploymentDevicePropertiesCollection, deploymentId));

            if (response != null && response.Items.Count > 0)
            {
                deploymentImpactedDevices.AddRange(response.Items.Select(this.CreateTwinServiceModel));
            }

            return new TwinServiceListModel(deploymentImpactedDevices);
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

        private async Task<bool> UpdateMetricsOfCurrentDeployment(string deviceGroupId, int priority)
        {
            var deploymentsFromHub = await this.ListAsync();

            var deploymentsOfDeviceGroup = deploymentsFromHub.Items.Where(i => i.DeviceGroupId == deviceGroupId).OrderByDescending(p => p.Priority).ThenByDescending(q => q.CreatedDateTimeUtc);

            if (deploymentsOfDeviceGroup != null && deploymentsOfDeviceGroup.Count() > 0)
            {
                var deployment = deploymentsOfDeviceGroup.First();

                if (priority >= deployment.Priority)
                {
                    var deploymentDetails = await this.GetDeploymentAsync(deployment.Id);

                    DeploymentServiceModel currentDeployment = await this.GetDeploymentFromStorageAsync(deployment.Id);

                    // Update the Device Statuses for the DeploymentId for future references.
                    currentDeployment.DeploymentMetrics = new DeploymentMetricsServiceModel(deploymentDetails.SystemMetrics, deploymentDetails.Metrics);

                    currentDeployment.DeploymentMetrics.DeviceStatuses = this.GetDeviceStatuses(deploymentDetails);

                    var deviceTwins = await this.GetDeviceProperties(currentDeployment.DeploymentMetrics.DeviceStatuses.Keys);

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

                    await this.client.UpdateAsync(DeploymentsCollection, currentDeployment.Id, value, currentDeployment.ETag);

                    await this.StoreDevicePropertiesInStorage(deviceTwins, currentDeployment.Id);

                    if (ConfigurationsHelper.IsEdgeDeployment(deploymentDetails))
                    {
                        var moduleTwins = await this.GetDeviceProperties(currentDeployment.DeploymentMetrics.DeviceStatuses.Keys, PackageType.EdgeManifest);
                        await this.StoreModuleTwinsInStorage(moduleTwins, currentDeployment.Id);
                    }

                    // Since the deployment that will be created have highest priority, mark it as the latest
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return true;
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

        private async Task<DeploymentServiceModel> MarkDeploymentAsInactive(DeploymentServiceModel existingDeployment, string userId, List<TwinServiceModel> deviceTwins, List<TwinServiceModel> moduleTwins, bool isDelete)
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

                if (deviceTwins != null && deviceTwins.Count > 0)
                {
                    await this.StoreDevicePropertiesInStorage(deviceTwins, existingDeployment.Id);
                }

                if (moduleTwins != null && moduleTwins.Count > 0)
                {
                    await this.StoreModuleTwinsInStorage(moduleTwins, existingDeployment.Id);
                }

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

        private async Task<List<TwinServiceModel>> GetDeviceProperties(IEnumerable<string> deviceIds, PackageType packageType = PackageType.DeviceConfiguration)
        {
            List<TwinServiceModel> twins = new List<TwinServiceModel>();
            string deviceQuery = @"deviceId IN [{0}]";
            string moduleQuery = @"deviceId IN [{0}] AND moduleId = '$edgeAgent'";

            if (deviceIds != null && deviceIds.Count() > 0)
            {
                var deviceIdsQuery = string.Join(",", deviceIds.Select(d => $"'{d}'"));
                if (packageType == PackageType.EdgeManifest)
                {
                    var query = string.Format(moduleQuery, deviceIdsQuery);

                    await this.GetModuleTwins(query, null, twins);
                }
                else
                {
                    var query = string.Format(deviceQuery, deviceIdsQuery);

                    await this.GetDeviceTwins(query, null, twins);
                }
            }

            return twins;
        }

        private async Task GetDeviceTwins(string query, string continuationToken, List<TwinServiceModel> twins)
        {
            DeviceServiceListModel devices = null;
            devices = await this.devices.GetListAsync(query, null);

            if (devices != null && devices.Items.Count() > 0)
            {
                twins.AddRange(devices.Items.Select(i => i.Twin));
                if (!string.IsNullOrWhiteSpace(devices.ContinuationToken))
                {
                    await this.GetDeviceTwins(query, continuationToken, twins);
                }
            }
        }

        private async Task GetModuleTwins(string query, string continuationToken, List<TwinServiceModel> twins)
        {
            TwinServiceListModel moduleTwins = null;
            moduleTwins = await this.devices.GetModuleTwinsByQueryAsync(query, null);

            if (moduleTwins != null && moduleTwins.Items.Count() > 0)
            {
                twins.AddRange(moduleTwins.Items);
                if (!string.IsNullOrWhiteSpace(moduleTwins.ContinuationToken))
                {
                    await this.GetModuleTwins(query, continuationToken, twins);
                }
            }
        }

        private async Task StoreDevicePropertiesInStorage(List<TwinServiceModel> deviceTwins, string deploymentId)
        {
            if (deviceTwins != null)
            {
                TwinServiceListModel existingDeviceTwins = await this.GetDeploymentDevicesAsync(deploymentId);
                if (existingDeviceTwins == null || (existingDeviceTwins != null && existingDeviceTwins.Items.Count == 0))
                {
                    foreach (var deviceTwin in deviceTwins)
                    {
                        await this.SaveDeviceTwin(deploymentId, deviceTwin, null);
                    }
                }
                else
                {
                    foreach (var deviceTwin in deviceTwins)
                    {
                        var existingDeviceTwin = existingDeviceTwins.Items.FirstOrDefault(x => x.DeviceId == deviceTwin.DeviceId);
                        await this.SaveDeviceTwin(deploymentId, deviceTwin, existingDeviceTwin?.ETag);

                        // archive exisiting Device Twin
                        var archiveDeviceTwinValue = JsonConvert.SerializeObject(
                            existingDeviceTwin,
                            Formatting.Indented,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore,
                            });
                        await this.client.UpdateAsync(string.Format(DeploymentHistoryPropertiesCollection, deploymentId, Guid.NewGuid().ToString()), deviceTwin.DeviceId, archiveDeviceTwinValue, null);
                    }
                }
            }
        }

        private async Task SaveDeviceTwin(string deploymentId, TwinServiceModel deviceTwin, string existingDeviceTwinEtag)
        {
            var value = JsonConvert.SerializeObject(
                deviceTwin,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                });

            await this.client.UpdateAsync(string.Format(DeploymentDevicePropertiesCollection, deploymentId), deviceTwin.DeviceId, value, existingDeviceTwinEtag);
        }

        private async Task SaveModuleTwin(string deploymentId, TwinServiceModel moduleTwin, string existingModuleTwinEtag)
        {
            var value = JsonConvert.SerializeObject(
                moduleTwin,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                });

            await this.client.UpdateAsync(string.Format(DeploymentEdgeModulePropertiesCollection, deploymentId), $"{moduleTwin.DeviceId}-{this.RemoveSpecialCharacters(moduleTwin.ModuleId)}", value, existingModuleTwinEtag);
        }

        private async Task StoreModuleTwinsInStorage(List<TwinServiceModel> moduleTwins, string deploymentId)
        {
            if (moduleTwins != null && moduleTwins.Count > 0)
            {
                TwinServiceListModel existingModuleTwins = await this.GetDeploymentImapactedModulesAsync(deploymentId);
                if (existingModuleTwins == null || (existingModuleTwins != null && existingModuleTwins.Items.Count == 0))
                {
                    foreach (var moduleTwin in moduleTwins)
                    {
                        await this.SaveModuleTwin(deploymentId, moduleTwin, null);
                    }
                }
                else
                {
                    foreach (var moduleTwin in moduleTwins)
                    {
                        var existingModuleTwin = existingModuleTwins.Items.FirstOrDefault(x => x.ModuleId == moduleTwin.ModuleId && x.DeviceId == moduleTwin.DeviceId);
                        await this.SaveModuleTwin(deploymentId, moduleTwin, existingModuleTwin?.ETag);

                        // archive exisiting Device Twin
                        var archiveModuleTwinValue = JsonConvert.SerializeObject(
                            existingModuleTwin,
                            Formatting.Indented,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore,
                            });
                        await this.client.UpdateAsync(string.Format(DeploymentHistoryPropertiesCollection, deploymentId, Guid.NewGuid().ToString()), $"{moduleTwin.DeviceId}-{this.RemoveSpecialCharacters(moduleTwin.ModuleId)}", archiveModuleTwinValue, null);
                    }
                }
            }
        }

        private string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            var validCharacters = "_-";
            foreach (char c in str)
            {
                if (char.IsLetterOrDigit(c) || validCharacters.Contains(c))
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}