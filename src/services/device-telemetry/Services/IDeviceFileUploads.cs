// <copyright file="IDeviceFileUploads.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Mmm.Iot.DeviceTelemetry.Services.Models;

namespace Mmm.Iot.DeviceTelemetry.Services
{
    public interface IDeviceFileUploads
    {
        Task<IEnumerable<DeviceUpload>> GetDeviceUploads(string tenantId, string deviceId);

        CloudBlockBlob Download(string tenantId, string blobName);
    }
}