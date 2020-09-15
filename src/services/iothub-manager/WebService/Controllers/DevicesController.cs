// <copyright file="DevicesController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mmm.Iot.Common.Services;
using Mmm.Iot.Common.Services.Filters;
using Mmm.Iot.IoTHubManager.Services;
using Mmm.Iot.IoTHubManager.WebService.Models;

namespace Mmm.Iot.IoTHubManager.WebService.Controllers
{
    [Route("v1/[controller]")]
    [TypeFilter(typeof(ExceptionsFilterAttribute))]
    public class DevicesController : Controller
    {
        private const string ContinuationTokenName = "x-ms-continuation";
        private readonly IDevices devices;
        private readonly IDeviceProperties deviceProperties;
        private readonly IDeviceService deviceService;

        public DevicesController(IDevices devices, IDeviceService deviceService, IDeviceProperties deviceProperties)
        {
            this.deviceProperties = deviceProperties;
            this.devices = devices;
            this.deviceService = deviceService;
        }

        [HttpGet]
        [Authorize("ReadAll")]
        public async Task<DeviceListApiModel> GetDevicesAsync([FromQuery] string query)
        {
            string continuationToken = string.Empty;
            if (this.Request.Headers.ContainsKey(ContinuationTokenName))
            {
                continuationToken = this.Request.Headers[ContinuationTokenName].FirstOrDefault();
            }

            return new DeviceListApiModel(await this.devices.GetListAsync(query, continuationToken));
        }

        [HttpPost("query")]
        [Authorize("ReadAll")]
        public async Task<DeviceListApiModel> QueryDevicesAsync([FromBody] string query)
        {
            string continuationToken = string.Empty;
            if (this.Request.Headers.ContainsKey(ContinuationTokenName))
            {
                continuationToken = this.Request.Headers[ContinuationTokenName].FirstOrDefault();
            }

            return new DeviceListApiModel(await this.devices.GetListAsync(query, continuationToken));
        }

        [HttpGet("deploymentHistory/{id}")]
        [Authorize("ReadAll")]
        public async Task<TwinPropertiesListApiModel> GetDeviceDeploymentAsync(string id)
        {
            return new TwinPropertiesListApiModel(await this.devices.GetDeploymentHistoryAsync(id, this.GetTenantId()));
        }

        [HttpGet("{id}")]
        [Authorize("ReadAll")]
        public async Task<DeviceRegistryApiModel> GetDeviceAsync(string id)
        {
            return new DeviceRegistryApiModel(await this.devices.GetAsync(id));
        }

        [HttpPost]
        [Authorize("CreateDevices")]
        public async Task<DeviceRegistryApiModel> PostAsync([FromBody] DeviceRegistryApiModel device)
        {
            return new DeviceRegistryApiModel(await this.devices.CreateAsync(device.ToServiceModel()));
        }

        [HttpPut("{id}")]
        [Authorize("UpdateDevices")]
        public async Task<DeviceRegistryApiModel> PutAsync(string id, [FromBody] DeviceRegistryApiModel device)
        {
            DevicePropertyDelegate updateListDelegate = new DevicePropertyDelegate(this.deviceProperties.UpdateListAsync);
            return new DeviceRegistryApiModel(await this.devices.UpdateAsync(device.ToServiceModel(), updateListDelegate));
        }

        [HttpDelete("{id}")]
        [Authorize("DeleteDevices")]
        public async Task DeleteAsync(string id)
        {
            await this.devices.DeleteAsync(id);
        }

        [HttpPost("{id}/methods")]
        [Authorize("CreateJobs")]
        public async Task<MethodResultApiModel> InvokeDeviceMethodAsync(string id, [FromBody] MethodParameterApiModel parameter)
        {
            return new MethodResultApiModel(await this.deviceService.InvokeDeviceMethodAsync(id, parameter.ToServiceModel()));
        }

        [HttpPost("{id}/c2dmessage")]
        [Authorize("SendC2DMessages")]
        public async Task SendCloudToDeviceAsync(string id, [FromBody] string message)
        {
            await this.deviceService.SendCloudToDeviceMessage(id, message);
        }

        [HttpGet("files/{id}")]
        [Authorize("ReadAll")]
        public async Task<List<string>> GetDeviceFilesAsync(string id)
        {
            return await this.deviceProperties.GetUploadedFilesForDevice(this.GetTenantId(), id);
        }
    }
}