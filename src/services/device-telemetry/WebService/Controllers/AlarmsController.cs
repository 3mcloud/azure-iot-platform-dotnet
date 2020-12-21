// <copyright file="AlarmsController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.Filters;
using Mmm.Iot.DeviceTelemetry.Services;
using Mmm.Iot.DeviceTelemetry.Services.Models;
using Mmm.Iot.DeviceTelemetry.WebService.Controllers.Helpers;
using Mmm.Iot.DeviceTelemetry.WebService.Models;

namespace Mmm.Iot.DeviceTelemetry.WebService.Controllers
{
    [TypeFilter(typeof(ExceptionsFilterAttribute))]
    public class AlarmsController : Controller
    {
        private const int DeleteLimit = 1000;
        private readonly IAlarms alarmService;
        private readonly ILogger logger;
        private readonly AppConfig appConfig;
        private int deviceLimit = 1000;

        public AlarmsController(
            IAlarms alarmService,
            ILogger<AlarmsController> logger,
            AppConfig appConfig)
        {
            this.alarmService = alarmService;
            this.logger = logger;
            this.appConfig = appConfig;
        }

        [HttpGet("v1/[controller]")]
        [Authorize("ReadAll")]
        public async Task<AlarmListApiModel> ListAsync(
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

            return await this.ListHelperAsync(from, to, order, skip, limit, deviceIds);
        }

        [HttpPost("v1/[controller]")]
        [Authorize("ReadAll")]
        public async Task<AlarmListApiModel> PostAsync([FromBody] QueryApiModel body)
        {
            string[] deviceIds = body.Devices == null
                ? new string[0]
                : body.Devices.ToArray();

            return await this.ListHelperAsync(
                body.From,
                body.To,
                body.Order,
                body.Skip,
                body.Limit,
                deviceIds);
        }

        [HttpGet("v1/[controller]/{id}")]
        [Authorize("ReadAll")]
        public async Task<AlarmApiModel> GetAsync([FromRoute] string id)
        {
            Alarm alarm = await this.alarmService.GetAsync(id);
            return new AlarmApiModel(alarm);
        }

        [HttpPatch("v1/[controller]/{id}")]
        [Authorize("UpdateAlarms")]
        public async Task<AlarmApiModel> PatchAsync(
            [FromRoute] string id,
            [FromBody] AlarmStatusApiModel body)
        {
            // validate input
            if (!(body.Status.Equals("open", StringComparison.OrdinalIgnoreCase) ||
                  body.Status.Equals("closed", StringComparison.OrdinalIgnoreCase) ||
                  body.Status.Equals("acknowledged", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidInputException(
                    "Status must be `closed`, `open`, or `acknowledged`." +
                    " Value provided:" + body.Status);
            }

            Alarm alarm = await this.alarmService.UpdateAsync(id, body.Status.ToLowerInvariant());
            return new AlarmApiModel(alarm);
        }

        [HttpDelete("v1/[controller]/{id}")]
        [Authorize("DeleteAlarms")]
        public async Task DeleteAsync([FromRoute] string id)
        {
            if (id == null)
            {
                throw new InvalidInputException("no id given to delete");
            }

            await this.alarmService.DeleteAsync(id);
        }

        [HttpPost("v1/[controller]!delete")]
        [Authorize("DeleteAlarms")]
        public void Delete([FromBody] AlarmIdListApiModel alarmList)
        {
            if (alarmList.Items == null || !alarmList.Items.Any())
            {
                throw new InvalidInputException("Must give list of at least 1 id to delete");
            }

            if (alarmList.Items.Count > DeleteLimit)
            {
                throw new InvalidInputException("Cannot delete more than 1000 alarms");
            }

            this.alarmService.Delete(alarmList.Items);
        }

        private async Task<AlarmListApiModel> ListHelperAsync(
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

            bool processIOTHubBeyondLimit = false;
            try
            {
                this.deviceLimit = this.appConfig.Global.Limits.MaximumDeviceCount;
                processIOTHubBeyondLimit = this.appConfig.Global.Limits.ProcessIOTHubBeyondLimit;
            }
            catch
            {
                this.deviceLimit = 1000;
                processIOTHubBeyondLimit = false;
            }

            if (!processIOTHubBeyondLimit && deviceIds.Length > this.deviceLimit)
            {
                this.logger.LogWarning("The client requested too many devices {count}", deviceIds.Length);
                throw new BadRequestException("The number of devices cannot exceed " + this.deviceLimit);
            }

            List<Alarm> alarmsList = await this.alarmService.ListAsync(
                fromDate,
                toDate,
                order,
                skip.Value,
                limit.Value,
                deviceIds);

            return new AlarmListApiModel(alarmsList);
        }
    }
}