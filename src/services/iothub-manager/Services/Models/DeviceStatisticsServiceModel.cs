// <copyright file="DeviceStatisticsServiceModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class DeviceStatisticsServiceModel
    {
        public DeviceStatisticsServiceModel()
        {
        }

        public int DeviceCount { get; set; }

        public int ConnectedDeviceCount { get; set; }
    }
}