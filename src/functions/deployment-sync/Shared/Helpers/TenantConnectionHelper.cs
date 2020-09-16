// <copyright file="TenantConnectionHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using Microsoft.Azure.Devices;
using Mmm.Iot.Functions.DeploymentSync.Shared.Exceptions;

namespace Mmm.Iot.Functions.DeploymentSync.Shared.Helpers
{
    public static class TenantConnectionHelper
    {
        private const string TenantIotHubConnectionStringFormat = "tenant:{0}:iotHubConnectionString";

        public static string GetIotHubConnectionString(string tenantId)
        {
            var appConfigurationKey = string.Format(TenantIotHubConnectionStringFormat, tenantId);

            string connectionString = Environment.GetEnvironmentVariable("AppConfigurationConnectionString", EnvironmentVariableTarget.Process);

            AppConfigHelper appConfigHelper = new AppConfigHelper(connectionString);
            return appConfigHelper.GetValue(appConfigurationKey);
        }

        public static RegistryManager GetRegistry(string tenantId)
        {
            RegistryManager registry = null;

            IoTHubConnectionHelper.CreateUsingHubConnectionString(GetIotHubConnectionString(tenantId), (conn) =>
            {
                registry = RegistryManager.CreateFromConnectionString(conn);
            });
            if (registry == null)
            {
                throw new InvalidConfigurationException($"Invalid tenant information for HubConnectionstring.");
            }

            return registry;
        }
    }
}