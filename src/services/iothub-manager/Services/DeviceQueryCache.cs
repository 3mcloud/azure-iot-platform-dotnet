// <copyright file="DeviceQueryCache.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.External.AppConfiguration;
using Mmm.Iot.Common.Services.External.CosmosDb;
using Mmm.Iot.Common.Services.Helpers;
using Mmm.Iot.IoTHubManager.Services.Models;

namespace Mmm.Iot.IoTHubManager.Services
{
    public class DeviceQueryCache : IDeviceQueryCache
    {
        private const string TimestampDocumentKey = "_timeReceived";
        private const string TwinChangeSchema = "twinchange";
        private const string TwinChangeDatabase = "iot";
        private const string AppConfigTenantInfoKey = "tenant";
        private const string AppConfigTwinChangeCollectionKey = "twin-change-collection";

        private readonly IStorageClient storage;
        private readonly IAppConfigurationClient tenantConfig;
        private readonly ILogger<IDeviceQueryCache> log;
        private IDictionary<string, DeviceQueryCacheServiceModel> cache = new Dictionary<string, DeviceQueryCacheServiceModel>();

        public DeviceQueryCache(IStorageClient storage, IAppConfigurationClient tenantConfig, ILogger<IDeviceQueryCache> log)
        {
            this.storage = storage;
            this.tenantConfig = tenantConfig;
            this.log = log;
        }

        public async Task<DeviceServiceListModel> GetCachedQueryResultAsync(string tenantId, string queryString)
        {
            DeviceQueryCacheResultServiceModel cachedResult;
            bool tenantHasCache = this.cache.TryGetValue(
                tenantId,
                out DeviceQueryCacheServiceModel cachedTenantValue);

            if (tenantHasCache)
            {
                bool cacheHasQuery = cachedTenantValue.QueryStringCache.TryGetValue(
                    queryString ?? string.Empty,
                    out cachedResult);

                if (!cacheHasQuery)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

            bool gotTwinChangeResult;
            List<Document> twinChangeResult = new List<Document>();

            try
            {
                // Get the latest twin change document that occurred after our query result time
                var sql = QueryBuilder.GetDocumentsSql(
                    TwinChangeSchema,
                    null,
                    null,
                    cachedResult?.ResultTimestamp,
                    TimestampDocumentKey,
                    DateTimeOffset.Now,
                    TimestampDocumentKey,
                    "DESC",  // gets the latest first
                    TimestampDocumentKey,
                    0,
                    1,
                    new string[0],  // all devices
                    "deviceId");

                twinChangeResult = await this.storage.QueryDocumentsAsync(
                    TwinChangeDatabase,
                    this.GetTwinChangeCollectionId(tenantId),
                    new FeedOptions
                    {
                        EnableCrossPartitionQuery = true,
                    },
                    sql,
                    0,
                    1);

                gotTwinChangeResult = true;
            }
            catch (Exception e)
            {
                this.log.LogInformation($"Unable to verify an IoT Hub Device Query Cache result because the twin-change CosmosDb collection for tenant {tenantId} could not be queried.", e);
                gotTwinChangeResult = false;
            }

            if (gotTwinChangeResult && (twinChangeResult == null || twinChangeResult.Count() == 0))
            {
                return cachedResult.Result;
            }
            else
            {
                this.ClearTenantCache(tenantId);
            }

            return null;
        }

        public void ClearTenantCache(string tenantId)
        {
            this.cache.Remove(tenantId);
        }

        public void SetTenantQueryResult(string tenantId, string queryString, DeviceQueryCacheResultServiceModel result)
        {
            var tenantHasCache = this.cache.TryGetValue(
                tenantId,
                out DeviceQueryCacheServiceModel tenantCache);

            if (tenantHasCache)
            {
                tenantCache.QueryStringCache[queryString] = result;
            }
            else
            {
                this.cache[tenantId] = new DeviceQueryCacheServiceModel
                {
                    QueryStringCache = new Dictionary<string, DeviceQueryCacheResultServiceModel>
                    {
                        { queryString ?? string.Empty, result },
                    },
                };
            }
        }

        private string GetTwinChangeCollectionId(string tenantId)
        {
            return this.tenantConfig.GetValue(
                $"{AppConfigTenantInfoKey}:{tenantId}:{AppConfigTwinChangeCollectionKey}");
        }
    }
}