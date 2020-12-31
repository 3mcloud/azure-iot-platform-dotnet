// <copyright file="StorageAdapterClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Net.Http;
using System.Threading.Tasks;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Helpers;

namespace Mmm.Iot.Common.Services.External.StorageAdapter
{
    public class StorageAdapterClient : ExternalServiceClient, IStorageAdapterClient
    {
        private readonly int timeout;

        public StorageAdapterClient(AppConfig config, IExternalRequestHelper requestHelper)
            : base(config.ExternalDependencies.StorageAdapterServiceUrl, requestHelper)
        {
            this.timeout = config.ExternalDependencies.StorageAdapterServiceTimeout;
        }

        public string RequestUrl(string path)
        {
            return $"{this.ServiceUri}/{path}";
        }

        public async Task<ValueApiModel> CreateAsync(
            string collectionId,
            string value)
        {
            return await this.CreateAsync(collectionId, value, null);
        }

        public async Task<ValueApiModel> CreateAsync(
            string collectionId,
            string value,
            string tenantId)
        {
            string url = this.RequestUrl($"collections/{collectionId}/values");
            ValueApiModel data = new ValueApiModel
            {
                Data = value,
            };
            return await this.RequestHelper.ProcessRequestAsync(HttpMethod.Post, url, data, tenantId);
        }

        public async Task<ValueApiModel> UpdateAsync(
            string collectionId,
            string key,
            string value,
            string etag)
        {
            return await this.UpdateAsync(collectionId, key, value, etag, null);
        }

        public async Task<ValueApiModel> UpdateAsync(
            string collectionId,
            string key,
            string value,
            string etag,
            string tenantId = null)
        {
            string url = this.RequestUrl($"collections/{collectionId}/values/{key}");
            ValueApiModel data = new ValueApiModel
            {
                Data = value,
                ETag = etag,
            };
            return await this.RequestHelper.ProcessRequestAsync(HttpMethod.Put, url, data, tenantId);
        }

        public async Task<ValueApiModel> GetAsync(
            string collectionId,
            string key)
        {
            return await this.GetAsync(collectionId, key, null);
        }

        public async Task<ValueApiModel> GetAsync(
            string collectionId,
            string key,
            string tenantId = null)
        {
            string url = this.RequestUrl($"collections/{collectionId}/values/{key}");
            return await this.RequestHelper.ProcessRequestAsync<ValueApiModel>(HttpMethod.Get, url, tenantId);
        }

        public async Task<ValueListApiModel> GetAllAsync(
            string collectionId)
        {
            return await this.GetAllAsync(collectionId, null);
        }

        public async Task<ValueListApiModel> GetAllAsync(
            string collectionId,
            string tenantId = null)
        {
            string url = this.RequestUrl($"collections/{collectionId}/values");
            return await this.RequestHelper.ProcessRequestAsync<ValueListApiModel>(HttpMethod.Get, url, tenantId);
        }

        public async Task DeleteAsync(
            string collectionId,
            string key)
        {
            await this.DeleteAsync(collectionId, key, null);
        }

        public async Task DeleteAsync(
            string collectionId,
            string key,
            string tenantId = null)
        {
            string url = this.RequestUrl($"collections/{collectionId}/values/{key}");
            await this.RequestHelper.ProcessRequestAsync(HttpMethod.Delete, url, tenantId: tenantId);
        }
    }
}