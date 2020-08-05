// <copyright file="ITenantContainer.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Mmm.Iot.TenantManager.Services.Models;

namespace Mmm.Iot.TenantManager.Services
{
    public interface ITenantContainer
    {
        Task<TenantModel> GetTenantAsync(string tenantGuid);

        Task<CreateTenantModel> CreateTenantAsync(string tenantGuid, string userId);

        Task<DeleteTenantModel> DeleteTenantAsync(string tenantGuid, string userId, bool ensureFullyDeployed = true);

        Task<bool> TenantIsReadyAsync(string tenantGuid);

        Task<TenantModel> UpdateTenantAsync(string tenantId, string tenantName);

        Task<UserTenantListModel> GetAllTenantsAsync(string userId);
    }
}