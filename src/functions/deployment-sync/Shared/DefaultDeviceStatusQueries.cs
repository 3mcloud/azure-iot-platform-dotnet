// <copyright file="DefaultDeviceStatusQueries.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class DefaultDeviceStatusQueries
    {
        public static IDictionary<QueryType, string> Queries { get; set; } = new Dictionary<QueryType, string>()
        {
            { QueryType.APPLIED, @"SELECT deviceId from devices where configurations.[[{0}]].status = 'Applied'" },
            { QueryType.SUCCESSFUL, string.Empty },
            { QueryType.FAILED, string.Empty },
        };
    }
}