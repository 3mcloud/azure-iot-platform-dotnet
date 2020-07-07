// <copyright file="DeviceFileApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Mmm.Iot.DeviceTelemetry.Services.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.DeviceTelemetry.WebService.Models
{
    public class DeviceFileApiModel
    {
        public DeviceFileApiModel(DeviceUpload deviceUpload)
        {
            this.Name = deviceUpload.Name;
            this.BlobName = deviceUpload.BlobName;
            this.UploadedBy = deviceUpload.UploadedBy;
            this.UploadedOn = deviceUpload.UploadedOn;
            this.Size = deviceUpload.Size;
        }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("BlobName")]
        public string BlobName { get; set; }

        [JsonProperty("UploadedBy")]
        public string UploadedBy { get; set; }

        [JsonProperty("UploadedOn")]
        public string UploadedOn { get; set; }

        [JsonProperty("Size")]
        public long Size { get; set; }
    }
}