// <copyright file="MessagesController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.TimeSeries;
using Mmm.Iot.Common.Services.Filters;
using Mmm.Iot.DeviceTelemetry.Services;
using Mmm.Iot.DeviceTelemetry.WebService.Controllers.Helpers;
using Mmm.Iot.DeviceTelemetry.WebService.Models;

namespace Mmm.Iot.DeviceTelemetry.WebService.Controllers
{
    [Route("v1/[controller]")]
    [TypeFilter(typeof(ExceptionsFilterAttribute))]
    public sealed class MessagesController : Controller
    {
        private const int DeviceLimit = 1000;
        private readonly IMessages messageService;
        private readonly ILogger logger;
        private readonly AppConfig config;

        public MessagesController(
            IMessages messageService,
            ILogger<MessagesController> logger,
            AppConfig config)
        {
            this.messageService = messageService;
            this.logger = logger;
            this.config = config;
        }

        [HttpGet]
        [Authorize("ReadAll")]
        public async Task<MessageListApiModel> GetAsync(
            [FromQuery] string from,
            [FromQuery] string to,
            [FromQuery] string order,
            [FromQuery] int? skip,
            [FromQuery] int? limit,
            [FromQuery] string devices)
        {
            string[] deviceIds = new string[0];
            if (devices != null)
            {
                deviceIds = devices.Split(',');
            }

            return await this.ListMessagesHelper(from, to, order, skip, limit, deviceIds);
        }

        [HttpGet("RecentMessages")]
        [Authorize("ReadAll")]
        public async Task<MessageListApiModel> GetTopDeviceMessagesAsync(
            [FromQuery] int? limit,
            [FromQuery] string deviceId)
        {
            MessageList messageList = await this.messageService.ListTopDeviceMessagesAsync(
                (limit == null || limit.Value > 10) ? 10 : limit.Value,
                deviceId);
            return new MessageListApiModel(messageList);
        }

        [HttpPost]
        [Authorize("ReadAll")]
        public async Task<MessageListApiModel> PostAsync([FromBody] QueryApiModel body)
        {
            string[] deviceIds = body.Devices == null
                ? new string[0]
                : body.Devices.ToArray();

            return await this.ListMessagesHelper(body.From, body.To, body.Order, body.Skip, body.Limit, deviceIds);
        }

        private async Task<MessageListApiModel> ListMessagesHelper(
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
                limit = this.config.DeviceTelemetryService.Messages.MessageRetrievalCountLimit;
            }

            // TODO: move this logic to the storage engine, depending on the
            // storage type the limit will be different. DEVICE_LIMIT is CosmosDb
            // limit for the IN clause.
            // if (deviceIds.Length > DeviceLimit)
            // {
            //     this.logger.LogWarning("The client requested too many devices {count}", deviceIds.Length);
            //     throw new BadRequestException("The number of devices cannot exceed " + DeviceLimit);
            // }
            MessageList messageList = await this.messageService.ListAsync(
                fromDate,
                toDate,
                order,
                skip.Value,
                limit.Value,
                deviceIds);

            return new MessageListApiModel(messageList);
        }
    }
}