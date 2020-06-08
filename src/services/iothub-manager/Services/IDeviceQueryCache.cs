// <copyright file="IDeviceQueryCache.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Mmm.Iot.IoTHubManager.Services.Models;

namespace Mmm.Iot.IoTHubManager.Services
{
    public interface IDeviceQueryCache
    {
        Task<DeviceServiceListModel> GetCachedQueryResultAsync(string tenantId, string queryString);

        void ClearTenantCache(string tenantId);

        void SetTenantQueryResult(string tenantId, string queryString, DeviceQueryCacheResultServiceModel result);
    }
}