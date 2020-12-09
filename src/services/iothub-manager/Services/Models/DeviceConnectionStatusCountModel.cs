// <copyright file="DeviceConnectionStatusCountModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Microsoft.Azure.Devices;

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class DeviceConnectionStatusCountModel
    {
        public DeviceConnectionStatusCountModel()
        {
        }

        public int NumberOfDevices { get; set; }

        public DeviceConnectionState ConnectionState { get; set; }
    }
}