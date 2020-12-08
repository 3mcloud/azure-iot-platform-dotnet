// <copyright file="TenantContainer.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.AppConfiguration;
using Mmm.Iot.Common.Services.External.BlobStorage;
using Mmm.Iot.Common.Services.External.CosmosDb;
using Mmm.Iot.Common.Services.External.TableStorage;
using Mmm.Iot.TenantManager.Services.External;
using Mmm.Iot.TenantManager.Services.Helpers;
using Mmm.Iot.TenantManager.Services.Models;

namespace Mmm.Iot.TenantManager.Services
{
    public class TenantContainer : ITenantContainer
    {
        private const string IotDatabaseId = "iot";
        private const string StorageAdapterDatabaseId = "pcs-storage";
        private const string TenantTableId = "tenant";
        private const string TenantOperationTable = "tenantOperations";
        private const string UserTableId = "user";
        private const string UserSettingsTableId = "userSettings";
        private const string LastUsedSettingKey = "LastUsedTenant";
        private const string CreatedRole = "[\"admin\"]";

        private readonly ILogger logger;
        private readonly IIdentityGatewayClient identityGatewayClient;
        private readonly IDeviceGroupsConfigClient deviceGroupClient;
        private readonly IRunbookHelper runbookHelper;
        private readonly IStorageClient cosmosClient;
        private readonly ITableStorageClient tableStorageClient;
        private readonly IAppConfigurationClient appConfigClient;
        private readonly IBlobStorageClient blobStorageClient;

        private readonly Dictionary<string, string> tenantCollections = new Dictionary<string, string>
        {
            { "telemetry", IotDatabaseId },
            { "twin-change", IotDatabaseId },
            { "lifecycle", IotDatabaseId },
            { "alarms", IotDatabaseId },
            { "pcs", StorageAdapterDatabaseId },
        };

        private readonly List<string> tenantBlobContainers = new List<string>
        {
            string.Empty,
            "-iot-file-upload",
        };

        private string iotHubNameFormat = "iothub-{0}";  // format with a guid
        private string dpsNameFormat = "dps-{0}";  // format with a guid
        private string streamAnalyticsNameFormat = "sa-{0}";  // format with a guide
        private string appConfigCollectionKeyFormat = "tenant:{0}:{1}-collection";  // format with a guid and collection name

        public TenantContainer(
            ILogger<TenantContainer> logger,
            IRunbookHelper runbookHelper,
            IStorageClient cosmosClient,
            ITableStorageClient tableStorageClient,
            IIdentityGatewayClient identityGatewayClient,
            IDeviceGroupsConfigClient deviceGroupConfigClient,
            IAppConfigurationClient appConfigHelper,
            IBlobStorageClient blobStorageClient)
        {
            this.logger = logger;
            this.runbookHelper = runbookHelper;
            this.cosmosClient = cosmosClient;
            this.tableStorageClient = tableStorageClient;
            this.identityGatewayClient = identityGatewayClient;
            this.deviceGroupClient = deviceGroupConfigClient;
            this.appConfigClient = appConfigHelper;
            this.blobStorageClient = blobStorageClient;
        }

        public async Task<bool> TenantIsReadyAsync(string tenantId)
        {
            // Load the tenant from table storage
            string partitionKey = tenantId.Substring(0, 1);
            TenantModel tenant = await this.tableStorageClient.RetrieveAsync<TenantModel>(TenantTableId, partitionKey, tenantId);
            return tenant != null && tenant.IsIotHubDeployed;  // True if the tenant's IoTHub is fully deployed, false otherwise
        }

        public async Task<CreateTenantModel> CreateTenantAsync(string tenantId, string userId)
        {
            /* Creates a new tenant */
            string iotHubName = this.FormatResourceName(this.iotHubNameFormat, tenantId);
            string dpsName = this.FormatResourceName(this.dpsNameFormat, tenantId);

            // Create a new tenant and save it to table storage
            var tenant = new TenantModel(tenantId);
            tenant.IotHubName = iotHubName;
            tenant.DpsName = dpsName;
            await this.tableStorageClient.InsertAsync<TenantModel>(TenantTableId, tenant);

            // Give the requesting user an admin role to the new tenant
            try
            {
                await this.identityGatewayClient.AddTenantForUserAsync(userId, tenantId, CreatedRole);
            }
            catch (Exception e)
            {
                throw new Exception("Unable to add user to tenant.", e);
            }

            // Update the userSettings table with the lastUsedTenant if there isn't already a lastUsedTenant
            IdentityGatewayApiSettingModel userSettings = null;

            try
            {
                userSettings = await this.identityGatewayClient.GetSettingsForUserAsync(userId, LastUsedSettingKey);
            }
            catch (Exception e)
            {
                throw new Exception("Could not access user settings for LastUsedTenant.", e);
            }

            if (userSettings == null)
            {
                // Set the last used tenant to be this new tenant
                try
                {
                    await this.identityGatewayClient.AddSettingsForUserAsync(userId, LastUsedSettingKey, tenantId);
                }
                catch (Exception e)
                {
                    throw new Exception("Could not set user settings for LastUsedTenant.", e);
                }
            }

            // Write tenant info cosmos db collection name to app config
            try
            {
                foreach (string collection in this.tenantCollections.Keys)
                {
                    string collectionKey = string.Format(this.appConfigCollectionKeyFormat, tenantId, collection);
                    string collectionId = $"{collection}-{tenantId}";
                    await this.appConfigClient.SetValueAsync(collectionKey, collectionId);
                }
            }
            catch (Exception e)
            {
                // In order for a complete tenant creation, all app config keys must be created. throw an error if not
                throw new Exception($"Unable to add required collection ids to App Config for tenant {tenantId}", e);
            }

            try
            {
                await this.deviceGroupClient.CreateDefaultDeviceGroupAsync(tenantId);
            }
            catch (Exception e)
            {
                throw new Exception("Unable to create the default device group for the new tenant.", e);
            }

            return new CreateTenantModel(tenantId);
        }

        public async Task<TenantModel> GetTenantAsync(string tenantId)
        {
            try
            {
                // Load the tenant from table storage
                var tenant = await this.tableStorageClient.RetrieveAsync<TenantModel>(TenantTableId, tenantId.Substring(0, 1), tenantId);
                return tenant;
            }
            catch (Exception e)
            {
                throw new Exception("Unable to retrieve the tenant from table storage", e);
            }
        }

        public async Task<DeleteTenantModel> DeleteTenantAsync(string tenantId, string userId, bool ensureFullyDeployed = true)
        {
            Dictionary<string, bool> deletionRecord = new Dictionary<string, bool> { };

            // Load the tenant from table storage
            string partitionKey = tenantId.Substring(0, 1);
            TenantModel tenant = await this.tableStorageClient.RetrieveAsync<TenantModel>(TenantTableId, partitionKey, tenantId);
            if (tenant != null && !tenant.IsIotHubDeployed && ensureFullyDeployed)
            {
                // If the tenant iothub is not deployed, we should not be able to start the delete process
                // this will mean the tenant is not fully deployed, so some resources could be deployed after
                // the delete process has begun
                throw new Exception("The tenant exists but it has not been fully deployed. Please wait for the tenant to fully deploy before trying to delete.");
            }
            else if (tenant == null)
            {
                this.logger.LogInformation("The tenant {tenantId} could not be deleted from Table Storage because it does not exist or was not fully created.", tenantId);
                deletionRecord["tenantTableStorage"] = true;
            }
            else
            {
                try
                {
                    await this.tableStorageClient.DeleteAsync<TenantModel>(TenantTableId, tenant);
                    deletionRecord["tenantTableStorage"] = true;
                }
                catch (Exception e)
                {
                    this.logger.LogInformation(e, "Unable to delete info from table storage for tenant {tenantId}", tenantId);
                    deletionRecord["tableStorage"] = false;
                }
            }

            // delete the tenant from the user
            try
            {
                await this.identityGatewayClient.DeleteTenantForAllUsersAsync(tenantId);
                deletionRecord["userTableStorage"] = true;
            }
            catch (Exception e)
            {
                this.logger.LogInformation(e, "Unable to delete user-tenant relationships for tenant {tenantId} in the user table.", tenantId);
                deletionRecord["userTableStorage"] = false;
            }

            // update userSettings table LastUsedTenant if necessary
            try
            {
                IdentityGatewayApiSettingModel lastUsedTenant = await this.identityGatewayClient.GetSettingsForUserAsync(userId, "LastUsedTenant");
                if (lastUsedTenant.Value == tenantId)
                {
                    // update the LastUsedTenant to some null
                    await this.identityGatewayClient.UpdateSettingsForUserAsync(userId, "LastUsedTenant", string.Empty);
                }
            }
            catch (Exception e)
            {
                this.logger.LogInformation(e, "Unable to get the user {userId} LastUsedTenant setting, the setting will not be updated.", userId);
            }

            // Gather tenant information
            string iotHubName = this.FormatResourceName(this.iotHubNameFormat, tenantId);
            string dpsName = this.FormatResourceName(this.dpsNameFormat, tenantId);

            // trigger delete iothub
            try
            {
                var result = await this.tableStorageClient.InsertAsync(TenantOperationTable, new TenantOperationModel(tenantId, TenantOperation.IoTHubDeletion, iotHubName));
                deletionRecord["iotHub"] = true;
            }
            catch (Exception e)
            {
                this.logger.LogInformation(e, "Unable to successfully add Delete IoTHub Operation for tenant {tenantId}", tenantId);
                deletionRecord["iotHub"] = false;
            }

            // trigger delete dps
            try
            {
                var result = await this.tableStorageClient.InsertAsync(TenantOperationTable, new TenantOperationModel(tenantId, TenantOperation.DpsDeletion, dpsName));
                deletionRecord["dps"] = true;
            }
            catch (Exception e)
            {
                this.logger.LogInformation(e, "Unable to successfully add Delete IoTHub Operation for tenant {tenantId}", tenantId);
                deletionRecord["dps"] = false;
            }

            string saJobName = this.FormatResourceName(this.streamAnalyticsNameFormat, tenantId);

            // trigger job deletion
            try
            {
                await this.tableStorageClient.InsertAsync(TenantOperationTable, new TenantOperationModel(tenantId, TenantOperation.SaJobDeletion, saJobName));
                deletionRecord["alerting"] = true;
            }
            catch (Exception e)
            {
                this.logger.LogInformation(e, "Unable to successfully add Delete Alerting Operation for tenant {tenantId}", tenantId);
                deletionRecord["alerting"] = false;
            }

            // Delete collections
            foreach (KeyValuePair<string, string> collectionInfo in this.tenantCollections)
            {
                string collection = collectionInfo.Key;
                string databaseId = collectionInfo.Value;
                string collectionAppConfigKey = string.Format(this.appConfigCollectionKeyFormat, tenantId, collection);
                string collectionId = string.Empty;
                try
                {
                    collectionId = this.appConfigClient.GetValue(collectionAppConfigKey);
                }
                catch (Exception e)
                {
                    this.logger.LogInformation(e, "Unable to retrieve the key {collectionKey} for a collection id in App Config for tenant {tenantId}", collectionAppConfigKey, tenantId);
                }

                if (string.IsNullOrEmpty(collectionId))
                {
                    this.logger.LogInformation("The collectionId was not set properly for tenant {tenantId} while attempting to delete the {collection} collection", collectionAppConfigKey, tenantId);

                    // Currently, the assumption for an unknown collection id is that it has been deleted.
                    // We can come to this conclusion by assuming that the app config key containing the collection id was already deleted.
                    // TODO: Determine a more explicit outcome for this scenario - jrb
                    deletionRecord[$"{collection}Collection"] = true;

                    // If the collectionId could not be properly retrieved, go on to the next colleciton, do not attempt to delete.
                    continue;
                }

                try
                {
                    await this.cosmosClient.DeleteCollectionAsync(databaseId, collectionId);
                    deletionRecord[$"{collection}Collection"] = true;
                }
                catch (ResourceNotFoundException e)
                {
                    this.logger.LogInformation(e, "The {collection} collection for tenant {tenantId} does exist and cannot be deleted.", collectionId, tenantId);
                    deletionRecord[$"{collection}Collection"] = true;
                }
                catch (Exception e)
                {
                    deletionRecord[$"{collection}Collection"] = false;
                    this.logger.LogInformation(e, "An error occurred while deleting the {collection} collection for tenant {tenantId}", collectionId, tenantId);
                }

                try
                {
                    // now that we have the collection Id, delete the key from app config
                    await this.appConfigClient.DeleteKeyAsync(collectionAppConfigKey);
                }
                catch (Exception e)
                {
                    this.logger.LogInformation(e, "Unable to delete {collectionKey} from App Config", collectionAppConfigKey, tenantId);
                }
            }

            foreach (string blobContainer in this.tenantBlobContainers)
            {
                string containerName = $"{tenantId}{blobContainer}";
                string deletionRecordValue = $"{containerName}BlobContainer";
                try
                {
                    await this.blobStorageClient.DeleteBlobContainerAsync(containerName);
                    deletionRecord[deletionRecordValue] = true;
                }
                catch (ResourceNotFoundException e)
                {
                    this.logger.LogInformation(e, $"Unable to delete blob container {containerName} for tenant {tenantId}. The blob container does not exist.");
                    deletionRecord[deletionRecordValue] = true;
                }
                catch (Exception e)
                {
                    this.logger.LogInformation(e, $"Unable to delete blob container {containerName} for tenant {tenantId}.");
                    deletionRecord[deletionRecordValue] = false;
                }
            }

            return new DeleteTenantModel(tenantId, deletionRecord, ensureFullyDeployed);
        }

        public async Task<TenantModel> UpdateTenantAsync(string tenantId, string tenantName)
        {
            try
            {
                // Load the tenant from table storage
                var tenant = await this.GetTenantAsync(tenantId);

                if (tenant == null)
                {
                    throw new ResourceNotFoundException($"Unable to fetch the tenant with TenantId:{tenantId}");
                }

                tenant.TenantName = tenantName;
                var updatedTenant = await this.tableStorageClient.InsertOrMergeAsync(TenantTableId, tenant);
                return updatedTenant;
            }
            catch (Exception e)
            {
                throw new Exception("Unable to update the tenant to table storage", e);
            }
        }

        public async Task<UserTenantListModel> GetAllTenantsAsync(string userId)
        {
            try
            {
                IdentityGatewayApiListModel identityGatewayUserTenants = await this.identityGatewayClient.GetAllTenantsForUserAsync(userId);

                UserTenantListModel userTenants = new UserTenantListModel(identityGatewayUserTenants);

                foreach (var userTenant in userTenants.Models)
                {
                    var tenantResponse = await this.GetTenantNameAsync(userTenant.TenantId);
                    userTenant.TenantName = tenantResponse != null && !string.IsNullOrWhiteSpace(tenantResponse.TenantName) ? tenantResponse.TenantName : $"tenant#{tenantResponse.TenantId.Substring(0, 5)}";
                    userTenant.IotHubName = tenantResponse != null && !string.IsNullOrWhiteSpace(tenantResponse.IotHubName) ? tenantResponse.IotHubName : string.Empty;
                }

                return userTenants;
            }
            catch (Exception e)
            {
                throw new Exception("Unable to retrieve the tenants", e);
            }
        }

        public async Task<TenantModel> GetTenantNameAsync(string tenantId)
        {
            try
            {
                TenantModel tenant = await this.GetTenantAsync(tenantId);
                return tenant;
            }
            catch (Exception e)
            {
                throw new Exception("Unable to retrieve the tenants", e);
            }
        }

        private string FormatResourceName(string format, string tenantId)
        {
            return string.Format(format, tenantId.Substring(0, 8));
        }
    }
}