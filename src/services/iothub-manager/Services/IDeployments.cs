// <copyright file="IDeployments.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.IoTHubManager.Services.Models;
using Mmm.Iot.StorageAdapter.Services.Models;

namespace Mmm.Iot.IoTHubManager.Services
{
    public interface IDeployments
    {
        Task<DeploymentServiceModel> CreateAsync(DeploymentServiceModel model, string userId, string tenantId);

        Task<DeploymentServiceListModel> ListAsync();

        Task<DeploymentServiceListModel> ListFromStorageAsync();

        Task<DeploymentServiceModel> GetAsync(string id, bool includeDeviceStatus, bool isLatest);

        Task DeleteAsync(string deploymentId, string userId, string tenantId, bool isDelete);

        Task<PackageApiModel> GetPackageAsync(string packageId);

        Task<DeviceGroup> GetDeviceGroupAsync(string deviceGroupId);

        Task ReactivateDeploymentAsyc(string deploymentId, string userId, string tenantId);

        Task<DeviceServiceListModel> GetDeployedDevicesAsync(string deploymentId, string tenantId, bool isLatest);

        Task<List<DeviceDeploymentStatusServiceModel>> GetDeploymentStatusReport(string id, string tenantId, bool isLatest = true);

        Task<TwinServiceListModel> GetModulesListAsync(string deploymentId, string query, bool isLatest);

        Task<IEnumerable<ValueServiceModel>> GetDeploymentHistory(string collectionId, string tenantId);

        Task<Dictionary<string, string>> GetDeployments(string collectionId, string tenantId);
    }
}