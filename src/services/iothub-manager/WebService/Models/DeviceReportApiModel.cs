// <copyright file="DeviceReportApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using Mmm.Iot.IoTHubManager.Services.Models;

namespace Mmm.Iot.IoTHubManager.WebService.Models
{
    public class DeviceReportApiModel
    {
        public DeviceReportApiModel(DeviceReportServiceModel device)
        {
            this.DeviceName = device.DeviceName;
            this.IsSimulated = device.IsSimulated;
            this.DeviceType = device.DeviceType;
            this.Firmware = device.Firmware;
            this.Status = device.Connected ? "Online" : "Offline";
            this.LastActivity = device.LastActivity;
            this.Telemetry = device.Telemetry;
        }

        public string DeviceName { get; set; }

        public bool IsSimulated { get; set; }

        public string DeviceType { get; set; }

        public string LastActivity { get; set; }

        public string Status { get; set; }

        public string Firmware { get; set; }

        public string Telemetry { get; set; }
    }
}