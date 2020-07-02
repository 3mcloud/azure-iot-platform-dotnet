// <copyright file="DeviceGroup.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Mmm.Iot.Config.Services.Models
{
    public class DeviceGroup
    {
        public string Id { get; set; }

        public string DisplayName { get; set; }

        public IEnumerable<DeviceGroupCondition> Conditions { get; set; }

        public IEnumerable<DeviceGroupSupportedMethods> SupportedMethods { get; set; }

        public IEnumerable<DeviceGroupTelemetryFormat> TelemetryFormat { get; set; }

        public string ETag { get; set; }
    }
}