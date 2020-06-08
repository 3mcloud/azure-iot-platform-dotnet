// <copyright file="DeviceQueryCacheResultServiceModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class DeviceQueryCacheResultServiceModel
    {
        public DeviceServiceListModel Result { get; set; }

        public DateTimeOffset ResultTimestamp { get; set; }
    }
}