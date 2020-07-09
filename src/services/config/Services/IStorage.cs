// <copyright file="IStorage.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Mmm.Iot.Common.Services.External.StorageAdapter;
using Mmm.Iot.Config.Services.External;
using Mmm.Iot.Config.Services.Models;
using Mmm.Platform.IoT.Common.Services.Models;

namespace Mmm.Iot.Config.Services
{
    public interface IStorage
    {
        Task<Theme> GetThemeAsync();

        Task<Theme> SetThemeAsync(object theme);

        Task<object> GetUserSetting(string id);

        Task<object> SetUserSetting(string id, object setting);

        Task<Logo> GetLogoAsync();

        Task<Logo> SetLogoAsync(Logo model);

        Task<ValueApiModel> SetSolutionSettingAsync(string id, object setting);

        Task<T> GetSolutionSettingAsync<T>(string id);

        Task<IEnumerable<DeviceGroup>> GetAllDeviceGroupsAsync();

        Task<ConfigTypeListServiceModel> GetConfigTypesListAsync();

        Task<DeviceGroup> GetDeviceGroupAsync(string id);

        Task<DeviceGroup> CreateDeviceGroupAsync(DeviceGroup input);

        Task<DeviceGroup> UpdateDeviceGroupAsync(string id, DeviceGroup input, string etag);

        Task DeleteDeviceGroupAsync(string id);

        Task<IEnumerable<PackageServiceModel>> GetAllPackagesAsync(IEnumerable<string> tags, string tagOperator);

        Task<PackageServiceModel> GetPackageAsync(string id);

        Task<IEnumerable<PackageServiceModel>> GetFilteredPackagesAsync(string packageType, string configType, IEnumerable<string> tags, string tagOperator);

        Task<PackageServiceModel> AddPackageAsync(PackageServiceModel package, string userId, string tenantId);

        Task DeletePackageAsync(string id, string userId, string tenantId);

        Task UpdateConfigTypeAsync(string customConfigType);

        Task<UploadFileServiceModel> UploadToBlobAsync(string tenantId, string filename, Stream stream = null);

        Task<PackageServiceModel> AddPackageTagAsync(string id, string tag, string userId);

        Task<PackageServiceModel> RemovePackageTagAsync(string id, string tag, string userId);
    }
}