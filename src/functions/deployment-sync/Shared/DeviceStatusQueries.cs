// <copyright file="DeviceStatusQueries.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public static class DeviceStatusQueries
    {
        private static Dictionary<string, IDictionary<QueryType, string>> admQueryMapping =
            new Dictionary<string, IDictionary<QueryType, string>>()
        {
            {
                ConfigType.Firmware.ToString(),
                FirmwareStatusQueries.Queries
            },
        };

        public static IDictionary<QueryType, string> GetQueries(string deploymentType, string configType)
        {
            if (deploymentType.Equals(PackageType.EdgeManifest.ToString()))
            {
                return EdgeDeviceStatusQueries.Queries;
            }

            return admQueryMapping.TryGetValue(
                configType,
                out IDictionary<QueryType, string> value) ? value : DefaultDeviceStatusQueries.Queries;
        }
    }
}