// <copyright file="DeviceStatisticsApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mmm.Iot.IoTHubManager.Services.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.IoTHubManager.WebService.Models
{
    public class DeviceStatisticsApiModel
    {
        public DeviceStatisticsApiModel()
        {
        }

        public DeviceStatisticsApiModel(DeviceStatisticsServiceModel deviceStatisticsServiceModel)
        {
            if (deviceStatisticsServiceModel != null)
            {
                this.DeviceCount = deviceStatisticsServiceModel.DeviceCount;
                this.ConnectedDeviceCount = deviceStatisticsServiceModel.ConnectedDeviceCount;
            }
        }

        [JsonProperty("DeviceCount")]
        public int DeviceCount { get; set; }

        [JsonProperty("ConnectedDeviceCount")]
        public int ConnectedDeviceCount { get; set; }
    }
}