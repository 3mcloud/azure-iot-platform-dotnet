// <copyright file="DeploymentsController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mmm.Iot.Common.Services;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.Filters;
using Mmm.Iot.IoTHubManager.Services;
using Mmm.Iot.IoTHubManager.Services.Models;
using Mmm.Iot.IoTHubManager.WebService.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.IoTHubManager.WebService.Controllers
{
    [Route("v1/[controller]")]
    [TypeFilter(typeof(ExceptionsFilterAttribute))]
    public class DeploymentsController : Controller
    {
        private readonly IDeployments deployments;

        public DeploymentsController(IDeployments deployments)
        {
            this.deployments = deployments;
        }

        [HttpPost]
        [Authorize("CreateDeployments")]
        public async Task<DeploymentApiModel> PostAsync([FromBody] DeploymentApiModel deployment)
        {
            if (string.IsNullOrWhiteSpace(deployment.Name))
            {
                throw new InvalidInputException("Name must be provided");
            }

            // If DeviceGroupId is provided, fill the DeviceGroup details if they are not provided
            if (!string.IsNullOrWhiteSpace(deployment.DeviceGroupId) && string.IsNullOrWhiteSpace(deployment.DeviceGroupQuery))
            {
                await this.HydrateDeploymentWithDeviceGroupDetails(deployment);
            }

            // If PackageId is provided, fill the package details if they are not provided
            if (!string.IsNullOrWhiteSpace(deployment.PackageId) && string.IsNullOrWhiteSpace(deployment.PackageContent))
            {
                await this.HydrateDeploymentWithPackageDetails(deployment);
            }

            if (string.IsNullOrWhiteSpace(deployment.DeviceGroupId))
            {
                throw new InvalidInputException("DeviceGroupId must be provided");
            }

            if (string.IsNullOrWhiteSpace(deployment.DeviceGroupName))
            {
                throw new InvalidInputException("DeviceGroupName must be provided");
            }

            if (string.IsNullOrWhiteSpace(deployment.DeviceGroupQuery) && (deployment.DeviceIds == null || (deployment.DeviceIds != null && deployment.DeviceIds.Count() == 0)))
            {
                throw new InvalidInputException("DeviceGroupQuery must be provided");
            }

            if (string.IsNullOrWhiteSpace(deployment.PackageContent))
            {
                throw new InvalidInputException("PackageContent must be provided");
            }

            if (deployment.PackageType.Equals(PackageType.DeviceConfiguration)
                && string.IsNullOrEmpty(deployment.ConfigType))
            {
                throw new InvalidInputException("Configuration type must be provided");
            }

            if (deployment.Priority < 0)
            {
                throw new InvalidInputException($"Invalid priority provided of {deployment.Priority}. " +
                                                "It must be non-negative");
            }

            return new DeploymentApiModel(await this.deployments.CreateAsync(deployment.ToServiceModel(), this.GetClaimsUserDetails(), this.GetTenantId()));
        }

        [HttpGet]
        [Authorize("ReadAll")]
        public async Task<DeploymentListApiModel> GetAsync()
        {
            return new DeploymentListApiModel(await this.deployments.ListFromStorageAsync());
        }

        [HttpGet("{id}")]
        [Authorize("ReadAll")]
        public async Task<DeploymentApiModel> GetAsync(string id, [FromQuery] bool includeDeviceStatus = false, [FromQuery] bool isLatest = true)
        {
            return new DeploymentApiModel(await this.deployments.GetAsync(id, includeDeviceStatus, isLatest));
        }

        [HttpDelete("{id}")]
        [Authorize("DeleteDeployments")]
        public async Task DeleteAsync(string id, [FromQuery] bool isDelete = true)
        {
            await this.deployments.DeleteAsync(id, this.GetClaimsUserDetails(), this.GetTenantId(), isDelete);
        }

        [HttpPut("{id}")]
        [Authorize("CreateDeployments")]
        public async Task ReactivateAsync(string id)
        {
            await this.deployments.ReactivateDeploymentAsyc(id, this.GetClaimsUserDetails(), this.GetTenantId());
        }

        [HttpPost("Devices/{id}")]
        [Authorize("ReadAll")]
        public async Task<DeviceListApiModel> GetDeploymentImpactedDevices(string id, [FromBody] string query, [FromQuery] bool isLatest = false)
        {
            return new DeviceListApiModel(await this.deployments.GetDeviceListAsync(id, query, isLatest));
        }

        private async Task HydrateDeploymentWithPackageDetails(DeploymentApiModel deployment)
        {
            var package = await this.deployments.GetPackageAsync(deployment.PackageId);

            if (package == null)
            {
                throw new ResourceNotFoundException($"No Package found with packageId:{deployment.PackageId}");
            }

            deployment.PackageType = package.PackageType;
            deployment.ConfigType = package.ConfigType;
            deployment.PackageContent = package.Content;
            deployment.PackageName = package.Name;
            deployment.DeviceGroupId = string.IsNullOrWhiteSpace(deployment.DeviceGroupId) ? Guid.Empty.ToString() : deployment.DeviceGroupId;
            deployment.DeviceGroupName = string.IsNullOrWhiteSpace(deployment.DeviceGroupName) ? deployment.DeviceIds != null && deployment.DeviceIds.Any() ? "DirectToDevices: " + string.Join(',', deployment.DeviceIds) : "DirectToDevices" : deployment.DeviceGroupName;
        }

        private async Task HydrateDeploymentWithDeviceGroupDetails(DeploymentApiModel deployment)
        {
            var deviceGroup = await this.deployments.GetDeviceGroupAsync(deployment.DeviceGroupId);

            if (deviceGroup == null)
            {
                throw new ResourceNotFoundException($"No Device Group found with DeviceGroupId:{deployment.DeviceGroupId}");
            }

            deployment.DeviceGroupName = deviceGroup.DisplayName;
            deployment.DeviceGroupQuery = JsonConvert.SerializeObject(deviceGroup.Conditions);
        }
    }
}