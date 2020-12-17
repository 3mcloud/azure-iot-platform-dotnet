// <copyright file="AlarmsByRuleController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.Filters;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.DeviceTelemetry.Services;
using Mmm.Iot.DeviceTelemetry.Services.Models;
using Mmm.Iot.DeviceTelemetry.WebService.Controllers.Helpers;
using Mmm.Iot.DeviceTelemetry.WebService.Models;

namespace Mmm.Iot.DeviceTelemetry.WebService.Controllers
{
    [Route("v1/[controller]")]
    [TypeFilter(typeof(ExceptionsFilterAttribute))]
    public class AlarmsByRuleController : Controller
    {
        private const int DeviceLimit = 1000;
        private readonly IAlarms alarmService;
        private readonly IRules ruleService;
        private readonly ILogger logger;

        public AlarmsByRuleController(
            IAlarms alarmService,
            IRules ruleService,
            ILogger<AlarmsByRuleController> logger)
        {
            this.alarmService = alarmService;
            this.ruleService = ruleService;
            this.logger = logger;
        }

        [HttpGet]
        [Authorize("ReadAll")]
        public async Task<AlarmByRuleListApiModel> GetAsync(
            [FromQuery] string from,
            [FromQuery] string to,
            [FromQuery] string order,
            [FromQuery] int? skip,
            [FromQuery] int? limit,
            [FromQuery] string devices)
        {
            string[] deviceIds = new string[0];
            if (!string.IsNullOrEmpty(devices))
            {
                deviceIds = devices.Split(',');
            }

            return await this.GetAlarmCountByRuleHelper(from, to, order, skip, limit, deviceIds);
        }

        [HttpPost]
        [Authorize("ReadAll")]
        public async Task<AlarmByRuleListApiModel> PostAsync([FromBody] QueryApiModel body)
        {
            string[] deviceIds = body.Devices == null
                ? new string[0]
                : body.Devices.ToArray();

            return await this.GetAlarmCountByRuleHelper(
                body.From,
                body.To,
                body.Order,
                body.Skip,
                body.Limit,
                deviceIds);
        }

        [HttpGet("{id}")]
        [Authorize("ReadAll")]
        public async Task<AlarmListByRuleApiModel> GetAsync(
            [FromRoute] string id,
            [FromQuery] string from,
            [FromQuery] string to,
            [FromQuery] string order,
            [FromQuery] int? skip,
            [FromQuery] int? limit,
            [FromQuery] string devices)
        {
            string[] deviceIds = new string[0];
            if (!string.IsNullOrEmpty(devices))
            {
                deviceIds = devices.Split(',');
            }

            return await this.GetAlarmListByRuleHelperAsync(id, from, to, order, skip, limit, deviceIds);
        }

        [HttpPost("{id}")]
        [Authorize("ReadAll")]
        public async Task<AlarmListByRuleApiModel> PostAsync(
            [FromRoute] string id,
            [FromBody] QueryApiModel body)
        {
            string[] deviceIds = body.Devices == null
                ? new string[0]
                : body.Devices.ToArray();

            return await this.GetAlarmListByRuleHelperAsync(
                id,
                body.From,
                body.To,
                body.Order,
                body.Skip,
                body.Limit,
                deviceIds);
        }

        private async Task<AlarmByRuleListApiModel> GetAlarmCountByRuleHelper(
            string from,
            string to,
            string order,
            int? skip,
            int? limit,
            string[] deviceIds)
        {
            DateTimeOffset? fromDate = DateHelper.ParseDate(from);
            DateTimeOffset? toDate = DateHelper.ParseDate(to);

            if (order == null)
            {
                order = "asc";
            }

            if (skip == null)
            {
                skip = 0;
            }

            if (limit == null)
            {
                limit = 1000;
            }

            /* TODO: move this logic to the storage engine, depending on the
             * storage type the limit will be different. DEVICE_LIMIT is CosmosDb
             * limit for the IN clause.
             */

            // if (deviceIds.Length > DeviceLimit)
            // {
            //     this.logger.LogWarning("The client requested too many devices {count}", deviceIds.Length);
            //     throw new BadRequestException("The number of devices cannot exceed " + DeviceLimit);
            // }
            List<AlarmCountByRule> alarmsList
                = await this.ruleService.GetAlarmCountForListAsync(
                    fromDate,
                    toDate,
                    order,
                    skip.Value,
                    limit.Value,
                    deviceIds);

            return new AlarmByRuleListApiModel(alarmsList);
        }

        private async Task<AlarmListByRuleApiModel> GetAlarmListByRuleHelperAsync(
            string id,
            string from,
            string to,
            string order,
            int? skip,
            int? limit,
            string[] deviceIds)
        {
            DateTimeOffset? fromDate = DateHelper.ParseDate(from);
            DateTimeOffset? toDate = DateHelper.ParseDate(to);

            if (order == null)
            {
                order = "asc";
            }

            if (skip == null)
            {
                skip = 0;
            }

            if (limit == null)
            {
                limit = 1000;
            }

            /* TODO: move this logic to the storage engine, depending on the
             * storage type the limit will be different. DEVICE_LIMIT is CosmosDb
             * limit for the IN clause.
             */

            // if (deviceIds.Length > DeviceLimit)
            // {
            //     this.logger.LogWarning("The client requested too many devices {count}", deviceIds.Length);
            //     throw new BadRequestException("The number of devices cannot exceed " + DeviceLimit);
            // }
            List<Alarm> alarmsList = await this.alarmService.ListByRuleAsync(
                id,
                fromDate,
                toDate,
                order,
                skip.Value,
                limit.Value,
                deviceIds);

            return new AlarmListByRuleApiModel(alarmsList);
        }
    }
}