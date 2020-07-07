// <copyright file="DeviceFileListApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mmm.Iot.DeviceTelemetry.Services.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.DeviceTelemetry.WebService.Models
{
    public class DeviceFileListApiModel
    {
        public DeviceFileListApiModel(IEnumerable<DeviceUpload> deviceUploads)
        {
            this.Items = new List<DeviceFileApiModel>();
            if (deviceUploads != null)
            {
                foreach (DeviceUpload deviceUpload in deviceUploads)
                {
                    this.Items.Add(new DeviceFileApiModel(deviceUpload));
                }
            }
        }

        [JsonProperty(PropertyName = "Items")]
        public List<DeviceFileApiModel> Items { get; set; }
    }
}