// <copyright file="EdgeDeviceStatusQueries.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class EdgeDeviceStatusQueries
    {
        public static IDictionary<QueryType, string> Queries { get; set; } = new Dictionary<QueryType, string>()
        {
            { QueryType.APPLIED, @"SELECT deviceId from devices.modules WHERE moduleId = '$edgeAgent' AND configurations.[[{0}]].status = 'Applied'" },
            { QueryType.SUCCESSFUL, @"SELECT deviceId from devices.modules WHERE moduleId = '$edgeAgent' AND configurations.[[{0}]].status = 'Applied' AND properties.desired.$version = properties.reported.lastDesiredVersion AND properties.reported.lastDesiredStatus.code = 200" },
            { QueryType.FAILED, @"SELECT deviceId FROM devices.modules WHERE moduleId = '$edgeAgent' AND configurations.[[{0}]].status = 'Applied' AND properties.desired.$version = properties.reported.lastDesiredVersion AND properties.reported.lastDesiredStatus.code != 200" },
        };
    }
}