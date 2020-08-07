// <copyright file="IStorageAdapterClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Threading.Tasks;

namespace Mmm.Iot.Common.Services.External.StorageAdapter
{
    public interface IStorageAdapterClient : IExternalServiceClient
    {
        Task<ValueApiModel> GetAsync(
            string collectionId,
            string key);

        Task<ValueApiModel> GetAsync(
            string collectionId,
            string key,
            string tenantId);

        Task<ValueListApiModel> GetAllAsync(
            string collectionId);

        Task<ValueListApiModel> GetAllAsync(
            string collectionId,
            string tenantId);

        Task<ValueApiModel> CreateAsync(
            string collectionId,
            string value);

        Task<ValueApiModel> CreateAsync(
            string collectionId,
            string value,
            string tenantId);

        Task DeleteAsync(
            string collectionId,
            string key);

        Task DeleteAsync(
            string collectionId,
            string key,
            string tenantId);

        Task<ValueApiModel> UpdateAsync(
            string collectionId,
            string key,
            string value,
            string etag);

        Task<ValueApiModel> UpdateAsync(
            string collectionId,
            string key,
            string value,
            string etag,
            string tenantId);
    }
}