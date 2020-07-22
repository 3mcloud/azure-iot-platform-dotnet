// <copyright file="DeviceGroupsController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mmm.Iot.AsaManager.Services;
using Mmm.Iot.AsaManager.Services.Models.IotHub;
using Mmm.Iot.AsaManager.WebService.Helpers;
using Mmm.Iot.Common.Services;
using Mmm.Iot.Common.Services.External.AsaManager;
using Mmm.Iot.Common.Services.Filters;
using Mmm.Iot.Common.Services.Wrappers;

namespace Mmm.Iot.AsaManager.WebService.Controllers
{
    [Route("v1/[controller]")]
    [TypeFilter(typeof(ExceptionsFilterAttribute))]
    public class DeviceGroupsController : Controller
    {
        private readonly IConverter deviceGroupConverter;
        private readonly IBeginConversionHelper beginConversionHelper;

        public DeviceGroupsController(
            DeviceGroupsConverter devicegroupConverter,
            IBeginConversionHelper beginConversionHelper)
        {
            this.deviceGroupConverter = devicegroupConverter;
            this.beginConversionHelper = beginConversionHelper;
        }

        [HttpPost("")]
        public BeginConversionApiModel BeginDeviceGroupConversion()
        {
            string tenantId = this.GetTenantId();
            string operationId = this.beginConversionHelper.BeginConversion(
                this.deviceGroupConverter.ConvertAsync,
                tenantId);

            // Return the operationId of the devicegroup conversion synchronous process
            return new BeginConversionApiModel
            {
                TenantId = tenantId,
                OperationId = operationId,
            };
        }

        [HttpPost("iothubjobdelay/{jobId}")]
        public BeginConversionApiModel BeginIotHubJobDelayDeviceGroupConversion(string jobId)
        {
            string tenantId = this.GetTenantId();
            string operationId = this.beginConversionHelper.WatchIotHubJobAndBeginConversion(
                jobId,
                this.deviceGroupConverter.ConvertAsync,
                tenantId);

            return new BeginConversionApiModel
            {
                TenantId = tenantId,
                OperationId = operationId,
            };
        }
    }
}