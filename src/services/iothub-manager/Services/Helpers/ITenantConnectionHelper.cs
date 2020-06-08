// <copyright file="ITenantConnectionHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Microsoft.Azure.Devices;

namespace Mmm.Iot.IoTHubManager.Services.Helpers
{
    public interface ITenantConnectionHelper
    {
        string TenantId { get; }

        string GetIotHubName();

        RegistryManager GetRegistry();

        string GetIotHubConnectionString();

        JobClient GetJobClient();
    }
}