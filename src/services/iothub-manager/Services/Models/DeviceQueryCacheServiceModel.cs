// <copyright file="DeviceQueryCacheServiceModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class DeviceQueryCacheServiceModel
    {
        public Dictionary<string, DeviceQueryCacheResultServiceModel> QueryStringCache { get; set; }
    }
}