// <copyright file="DeviceStatisticsServiceModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class DeviceStatisticsServiceModel
    {
        public DeviceStatisticsServiceModel()
        {
        }

        public DeviceStatisticsServiceModel(List<DeviceConnectionStatusCountModel> data)
        {
            foreach (var item in data)
            {
                switch (item.ConnectionState)
                {
                    case Microsoft.Azure.Devices.DeviceConnectionState.Disconnected:
                        this.TotalDeviceCount = this.TotalDeviceCount + item.NumberOfDevices;
                        break;
                    case Microsoft.Azure.Devices.DeviceConnectionState.Connected:
                        this.TotalDeviceCount = this.TotalDeviceCount + item.NumberOfDevices;
                        this.ConnectedDeviceCount = item.NumberOfDevices;
                        break;
                    default:
                        break;
                }
            }
        }

        public int TotalDeviceCount { get; set; }

        public int ConnectedDeviceCount { get; set; }
    }
}