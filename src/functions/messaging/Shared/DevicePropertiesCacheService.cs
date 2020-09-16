// <copyright file="DevicePropertiesCacheService.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Mmm.Iot.Functions.Messaging.Shared.Exceptions;
using Mmm.Iot.Functions.Messaging.Shared.Helpers;
using Newtonsoft.Json;

namespace Mmm.Iot.Functions.Messaging.Shared
{
    public class DevicePropertiesCacheService
    {
        private const string CollectionId = "device-twin-properties";
        private const string Key = "cache";
        private const string TenantKey = "tenant:";
        private const string CollectionKey = "pcs";
        private const string CosmosConnectionStringKey = "Global:CosmosDb:documentDbConnectionString";
        private const string CosmosDocumentDbKey = "StorageAdapter:documentDb";
        private const string CosmosDbRus = "StorageAdapter:documentdbrus";
        private const string DevicePropertyWhitelist = "IothubManagerService:DevicePropertiesCache:whitelist";
        private string cosmosConnectionString;
        private string cosmosDbcoll;
        private string cosmosDb;
        private int cosmosDbRus = 400;
        private string whitelist;

        public async Task<bool> UpdateDevicePropertiesAsync(string eventData, string tenant)
        {
            bool updateStatus = false;
            DevicePropertyServiceModel existingDevProperties = null;
            ValueServiceModel deviceCacheValue = null;
            CosmosOperations docClient = null;

            try
            {
                this.cosmosDbcoll = $"{CollectionKey}-{tenant}";
                this.cosmosDb = Environment.GetEnvironmentVariable("DevicePropertiesCacheDatabaseId", EnvironmentVariableTarget.Process);
                this.whitelist = Environment.GetEnvironmentVariable("WhiteList", EnvironmentVariableTarget.Process);
                this.cosmosDbRus = Convert.ToInt32(Environment.GetEnvironmentVariable("CosmosDBRus", EnvironmentVariableTarget.Process));
                this.cosmosConnectionString = Environment.GetEnvironmentVariable("CosmosDbConnectionString", EnvironmentVariableTarget.Process);

                docClient = await CosmosOperations.GetClientAsync();
                updateStatus = await docClient.CreateCollectionIfNotExistsAsync(this.cosmosDb, this.cosmosDbcoll, this.cosmosDbRus, CosmosOperation.DeviceCache);

                // CosmosHelper cosmosHelper = new CosmosHelper(cosmosConnectionString, cosmosDbcoll, cosmosDb, tenant, cosmosDbRus, CosmosOperation.devicecache);
                try
                {
                    deviceCacheValue = await docClient.GetDocumentAsync(this.GenerateCollectionLink(this.cosmosDb, this.cosmosDbcoll), CollectionId, Key);
                    existingDevProperties = JsonConvert.DeserializeObject<DevicePropertyServiceModel>(deviceCacheValue.Data);
                }
                catch (ResourceNotFoundException)
                {
                    // Do nothing
                }
                catch (Exception exception)
                {
                    throw new ApplicationException("Get Device Properties failed", exception);
                }

                DeviceTwinNameOperations devTwinOps = new DeviceTwinNameOperations();
                DeviceTwinName deviceTwinName = devTwinOps.GetValidNames(this.whitelist, existingDevProperties, eventData);

                if (deviceTwinName != null)
                {
                    DevicePropertyServiceModel devicePropertyModel = new DevicePropertyServiceModel
                    {
                        Tags = deviceTwinName.Tags,
                        Reported = deviceTwinName.ReportedProperties,
                    };

                    ValueServiceModel valueSvcModel = new ValueServiceModel(new KeyValueDocument(CollectionId, Key, JsonConvert.SerializeObject(devicePropertyModel)));

                    if (devicePropertyModel != null && existingDevProperties == null)
                    {
                        // Create the document
                        if (deviceCacheValue == null)
                        {
                            await docClient.SaveDocumentAsync(CollectionId, Key, valueSvcModel, this.GenerateCollectionLink(this.cosmosDb, this.cosmosDbcoll));
                            updateStatus = true;
                        }
                    }
                    else
                    {
                        // update only if the Tags or the reported properties are different from ones stored in cosmos
                        if (existingDevProperties != null && (!existingDevProperties.Tags.SetEquals(devicePropertyModel.Tags) || !existingDevProperties.Reported.SetEquals(devicePropertyModel.Reported)))
                        {
                            await docClient.SaveDocumentAsync(CollectionId, Key, valueSvcModel, this.GenerateCollectionLink(this.cosmosDb, this.cosmosDbcoll));
                            updateStatus = true;
                        }
                    }
                }
                else
                {
                    updateStatus = true;
                }
            }
            catch (Exception exception)
            {
                throw new ApplicationException($"Update Device Properties failed {exception}, tenant: {tenant}");
            }

            return updateStatus;
        }

        private string GenerateCollectionLink(string cosmosDatabase, string cosmosCollection)
        {
            return $"/dbs/{cosmosDatabase}/colls/{cosmosCollection}";
        }

        private bool GetConfigValues(string tenant, string appConfigConnection)
        {
            bool result = false;
            try
            {
                AppConfigHelper appConfig = new AppConfigHelper(appConfigConnection);
                this.cosmosConnectionString = appConfig.GetValue(CosmosConnectionStringKey);
                this.cosmosDbcoll = appConfig.GetValue($"{TenantKey}{tenant}:{CollectionKey}");
                this.cosmosDb = appConfig.GetValue(CosmosDocumentDbKey);
                this.cosmosDbRus = Convert.ToInt32(appConfig.GetValue(CosmosDbRus));
                this.whitelist = appConfig.GetValue(DevicePropertyWhitelist);
                result = true;
            }
            catch (Exception)
            {
                // log exception
            }

            return result;
        }
    }
}