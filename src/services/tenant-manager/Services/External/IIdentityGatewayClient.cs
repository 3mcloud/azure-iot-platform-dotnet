// <copyright file="IIdentityGatewayClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Mmm.Iot.Common.Services.External;
using Mmm.Iot.TenantManager.Services.Models;

namespace Mmm.Iot.TenantManager.Services.External
{
    public interface IIdentityGatewayClient : IExternalServiceClient
    {
        Task<IdentityGatewayApiModel> AddTenantForUserAsync(string userId, string tenantId, string roles, string name = null, string createdBy = null);

        Task<IdentityGatewayApiModel> GetTenantForUserAsync(string userId, string tenantId);

        Task<IdentityGatewayApiSettingModel> AddSettingsForUserAsync(string userId, string settingKey, string settingValue);

        Task<IdentityGatewayApiSettingModel> GetSettingsForUserAsync(string userId, string settingKey);

        Task<IdentityGatewayApiSettingModel> UpdateSettingsForUserAsync(string userId, string settingKey, string settingValue);

        Task<IdentityGatewayApiModel> DeleteTenantForAllUsersAsync(string tenantId);

        Task<IdentityGatewayApiListModel> GetAllTenantsForUserAsync(string userId);

        Task<IdentityGatewayApiListModel> GetAllSystemAdminsAsync();
    }
}