// <copyright file="IIotHubManagerClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Mmm.Iot.AsaManager.Services.Models.DeviceGroups;
using Mmm.Iot.AsaManager.Services.Models.IotHub;
using Mmm.Iot.Common.Services.External;

namespace Mmm.Iot.AsaManager.Services.External.IotHubManager
{
    public interface IIotHubManagerClient : IExternalServiceClient
    {
        Task<DeviceListModel> GetListAsync(IEnumerable<DeviceGroupConditionModel> conditions, string tenantId);

        Task<JobModel> GetJobAsync(string jobId, string tenantId);
    }
}