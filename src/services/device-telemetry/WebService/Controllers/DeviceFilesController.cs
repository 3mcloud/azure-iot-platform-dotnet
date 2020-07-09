// <copyright file="DeviceFilesController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mmm.Iot.Common.Services;
using Mmm.Iot.Common.Services.Filters;
using Mmm.Iot.DeviceTelemetry.Services;
using Mmm.Iot.DeviceTelemetry.Services.Models;
using Mmm.Iot.DeviceTelemetry.WebService.Models;

namespace Mmm.Iot.DeviceTelemetry.WebService.Controllers
{
    [Route("v1/[controller]")]
    [TypeFilter(typeof(ExceptionsFilterAttribute))]
    public class DeviceFilesController : Controller
    {
        private readonly IDeviceFileUploads deviceFileUploads;

        public DeviceFilesController(IDeviceFileUploads deviceFileUploads)
        {
            this.deviceFileUploads = deviceFileUploads;
        }

        [HttpGet("{deviceId}")]
        [Authorize("ReadAll")]
        public async Task<DeviceFileListApiModel> GetDeviceUploads(string deviceId)
        {
            var deviceFiles = await this.deviceFileUploads.GetDeviceUploads(this.GetTenantId(), deviceId);

            return new DeviceFileListApiModel(deviceFiles);
        }

        [HttpPost("Download")]
        [Authorize("ReadAll")]
        public async Task<IActionResult> GetFileContents([FromBody]DownloadRequest downloadRequest)
        {
            var blob = this.deviceFileUploads.Download(this.GetTenantId(), downloadRequest.BlobName);
            Stream blobStream = await blob.OpenReadAsync();
            return this.File(blobStream, blob.Properties.ContentType, blob.Name);
        }
    }
}