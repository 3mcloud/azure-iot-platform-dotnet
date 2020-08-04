// <copyright file="ITenantManagerClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Mmm.Iot.Common.Services.External;
using Mmm.Iot.IdentityGateway.Services.Models;

namespace Mmm.Iot.IdentityGateway.Services.External
{
    public interface ITenantManagerClient : IExternalServiceClient
    {
        Task<TenantListModel> GetAllActiveTenantAsync();
    }
}