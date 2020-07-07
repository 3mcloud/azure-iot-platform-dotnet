// <copyright file="DeviceUpload.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Linq;
using Newtonsoft.Json;

namespace Mmm.Iot.DeviceTelemetry.Services.Models
{
    public class DeviceUpload
    {
        public string BlobName { get; set; }

        public string Name
        {
            get { return this.BlobName.Split('/').Last(); }
        }

        public string UploadedBy { get; set; }

        public string UploadedOn { get; set; }

        public long Size { get; set; }
    }
}