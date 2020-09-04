// <copyright file="FirmwareStatusQueries.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using static Mmm.Iot.Config.Services.Models.DeviceStatusQueries;

namespace Mmm.Iot.Config.Services.Models
{
    public class FirmwareStatusQueries
    {
        public static IDictionary<QueryType, string> Queries { get; set; } = new Dictionary<QueryType, string>()
        {
            { QueryType.APPLIED, @"SELECT deviceId from devices where configurations.[[{0}]].status = 'Applied'" },
            { QueryType.SUCCESSFUL, @"SELECT deviceId FROM devices WHERE configurations.[[{0}]].status = 'Applied' AND (properties.reported.firmware.fwUpdateStatus='Current' OR properties.reported.firmware.fwUpdateStatus='current')" },
            { QueryType.FAILED, @"SELECT deviceId FROM devices WHERE configurations.[[{0}]].status = 'Applied' AND (properties.reported.firmware.fwUpdateStatus='Error' OR properties.reported.firmware.fwUpdateStatus='error')" },
        };
    }
}