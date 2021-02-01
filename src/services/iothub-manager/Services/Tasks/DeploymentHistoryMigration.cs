// <copyright file="DeploymentHistoryMigration.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Hosting;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.External.AppConfiguration;
using Mmm.Iot.Common.Services.External.CosmosDb;
using Mmm.Iot.Common.Services.External.StorageAdapter;
using Mmm.Iot.Common.Services.External.TableStorage;
using Mmm.Iot.IoTHubManager.Services.Models;
using Mmm.Iot.StorageAdapter.Services.Helpers;
using Mmm.Iot.StorageAdapter.Services.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.IoTHubManager.Services.Tasks
{
    public class DeploymentHistoryMigration : IHostedService, IDisposable
    {
        private const string DeploymentHistoryPropertiesCollection = "deploymentHistory";
        private const string DeploymentDevicePropertiesCollection = "deploymentdevices";
        private const string DeviceDeploymentHistory = "deviceDeploymentHistory-{0}";
        private const string TenantTable = "tenant";
        private const string TenantMigrationStatus = "tenantMigrationStatus";
        private const string AppConfigTenantInfoKey = "tenant";
        private const string AppConfigPcsCollectionKey = "pcs-collection";
        private readonly CancellationTokenSource stoppingCts = new CancellationTokenSource();
        private readonly IStorageClient client;
        private readonly ITableStorageClient tableStorageClient;
        private readonly AppConfig config;
        private readonly IDeployments deployments;
        private readonly IAppConfigurationClient appConfigurationClient;
        private Task executingTask;

        public DeploymentHistoryMigration(IStorageClient client, ITableStorageClient tableStorageClient, AppConfig config, IDeployments deployments, IAppConfigurationClient appConfigurationClient)
        {
            this.client = client;
            this.tableStorageClient = tableStorageClient;
            this.config = config;
            this.deployments = deployments;
            this.appConfigurationClient = appConfigurationClient;
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
                // Get the list of tenants
                TableQuery<TenantMigrationModel> query = new TableQuery<TenantMigrationModel>().Where(TableQuery.GenerateFilterConditionForBool("IsIotHubDeployed", QueryComparisons.Equal, true));
                List<TenantMigrationModel> tenants = await this.tableStorageClient.QueryAsync<TenantMigrationModel>(TenantTable, query);

                // Verify against the list of processed tenants
                TableQuery<TenantMigrationModel> processedTenantsQuery = new TableQuery<TenantMigrationModel>().Where(TableQuery.GenerateFilterConditionForBool(nameof(TenantMigrationModel.IsMigrationCompleted), QueryComparisons.Equal, true));

                List<TenantMigrationModel> processedTenants = await this.tableStorageClient.QueryAsync<TenantMigrationModel>(TenantMigrationStatus, processedTenantsQuery);

                var tenantsToMigrate = tenants.Select(t => t.TenantId).Except(processedTenants.Select(t => t.TenantId));

                if (tenantsToMigrate != null && tenantsToMigrate.Count() > 0)
                {
                    string tenantToMigrate = tenantsToMigrate.First();

                    // Get Deployments for Name
                    Dictionary<string, string> deployments = await this.GetDeployments(tenantToMigrate, "deployments");

                    bool isTenantMigrationCompleted = false;
                    while (!isTenantMigrationCompleted)
                    {
                        // get the data from tenant for deploymentdevices collection
                        IEnumerable<ValueServiceModel> deploymentImpactedDevicesData = await this.GetDataForMigration(tenantToMigrate, DeploymentDevicePropertiesCollection);

                        if (deploymentImpactedDevicesData != null && deploymentImpactedDevicesData.Count() > 0)
                        {
                            // Convert the data into new model to be saved
                            foreach (ValueServiceModel data in deploymentImpactedDevicesData)
                            {
                                DeploymentHistoryModel deviceDeploymentHistory = new DeploymentHistoryModel();
                                deviceDeploymentHistory.DeploymentId = data.CollectionId.Substring(18);
                                deviceDeploymentHistory.DeviceId = data.Key;
                                deviceDeploymentHistory.DeploymentName = deployments.ContainsKey(deviceDeploymentHistory.DeploymentId) ? deployments[deviceDeploymentHistory.DeploymentId] : string.Empty;
                                deviceDeploymentHistory.LastUpdatedDateTimeUtc = data.Timestamp.UtcDateTime;
                                deviceDeploymentHistory.PreviousFirmwareTwin = null;
                                deviceDeploymentHistory.Twin = JsonConvert.DeserializeObject<TwinServiceModel>(data.Data);

                                // save the record to new collection
                                var isSuccess = await this.SaveDataToStorage(string.Format(DeviceDeploymentHistory, deviceDeploymentHistory.DeviceId), deviceDeploymentHistory.DeploymentId, deviceDeploymentHistory, tenantToMigrate);

                                if (isSuccess)
                                {
                                    // Delete the existing data once saved.
                                    await this.DeleteDataFromStorage(tenantToMigrate, data.CollectionId, data.Key);
                                }
                            }
                        }
                        else
                        {
                            IEnumerable<ValueServiceModel> deploymentImpactedDevicesHistoryData = await this.GetDataForMigration(tenantToMigrate, DeploymentHistoryPropertiesCollection);
                            if (deploymentImpactedDevicesHistoryData != null && deploymentImpactedDevicesHistoryData.Count() > 0)
                            {
                                // Convert the data into new model to be saved
                                foreach (ValueServiceModel data in deploymentImpactedDevicesData)
                                {
                                    DeploymentHistoryModel deviceDeploymentHistory = new DeploymentHistoryModel();
                                    deviceDeploymentHistory.DeploymentId = data.CollectionId.Substring(18, data.CollectionId.Length - (36 + 1) - 18);
                                    deviceDeploymentHistory.DeviceId = data.Key;
                                    deviceDeploymentHistory.DeploymentName = deployments.ContainsKey(deviceDeploymentHistory.DeploymentId) ? deployments[deviceDeploymentHistory.DeploymentId] : string.Empty;
                                    deviceDeploymentHistory.LastUpdatedDateTimeUtc = data.Timestamp.UtcDateTime;
                                    deviceDeploymentHistory.PreviousFirmwareTwin = null;
                                    deviceDeploymentHistory.Twin = JsonConvert.DeserializeObject<TwinServiceModel>(data.Data);

                                    // save the record to new collection
                                    var isSuccess = await this.SaveDataToStorage(string.Format(DeviceDeploymentHistory, deviceDeploymentHistory.DeviceId), deviceDeploymentHistory.DeploymentId, deviceDeploymentHistory, tenantToMigrate);

                                    if (isSuccess)
                                    {
                                        // Delete the existing data once saved.
                                        await this.DeleteDataFromStorage(tenantToMigrate, data.CollectionId, data.Key);
                                    }
                                }
                            }
                            else
                            {
                                isTenantMigrationCompleted = true;
                                break;
                            }
                        }

                        await Task.Delay(30 * 1000, stoppingToken);
                    }

                    // Update the TenantMigrationStatus to true;
                    await this.InsertTenantMigrationStatus(tenantToMigrate);
                }
                else
                {
                    // Since there are no more tenants to process, cancel the token to stop the process.
                    // Cancel the process
                    Console.WriteLine("Stopping the process");
                    this.stoppingCts.Cancel();
                }

                await Task.Delay(30 * 1000, stoppingToken);
            }
        }

        private async Task<IEnumerable<ValueServiceModel>> GetDataForMigration(string tenantId, string collectionId)
        {
            return await this.deployments.GetDeploymentHistory(collectionId, tenantId);
        }

        private async Task<bool> SaveDataToStorage(string collectionId, string key, DeploymentHistoryModel deploymentHistory, string tenantId)
        {
            bool isSuccess = false;
            var value = JsonConvert.SerializeObject(
                deploymentHistory,
                Formatting.None,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                });

            KeyValueDocument document = new KeyValueDocument(collectionId, key, value, Guid.NewGuid().ToString());

            try
            {
                await this.client.CreateDocumentAsync(this.DocumentDbDatabaseId, this.GetPcsCollectionId(tenantId), document);
                isSuccess = true;
            }
            catch (Exception)
            {
            }

            return isSuccess;
        }

        private async Task DeleteDataFromStorage(string tenantId, string collectionId, string key)
        {
            string docId = DocumentIdHelper.GenerateId(collectionId, key);
            try
            {
                await this.client.DeleteDocumentAsync(this.DocumentDbDatabaseId, this.GetPcsCollectionId(tenantId), docId);
            }
            catch (Exception)
            {
            }
        }

        private async Task InsertTenantMigrationStatus(string tenantId)
        {
            // Create a new tenant and save it to table storage
            var migratedTenant = new TenantMigrationModel(tenantId, true);
            await this.tableStorageClient.InsertAsync<TenantMigrationModel>(TenantMigrationStatus, migratedTenant);
        }

        private async Task<Dictionary<string, string>> GetDeployments(string tenantId, string collectionId)
        {
            return await this.deployments.GetDeployments(collectionId, tenantId);
        }

        private string GetPcsCollectionId(string tenantId)
        {
            return this.appConfigurationClient.GetValue(
                $"{AppConfigTenantInfoKey}:{tenantId}:{AppConfigPcsCollectionKey}");
        }
    }
}