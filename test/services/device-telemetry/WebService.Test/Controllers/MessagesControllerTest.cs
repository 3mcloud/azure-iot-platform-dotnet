// <copyright file="MessagesControllerTest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.External.AppConfiguration;
using Mmm.Iot.Common.Services.External.TimeSeries;
using Mmm.Iot.Common.Services.Http;
using Mmm.Iot.DeviceTelemetry.Services;
using Mmm.Iot.DeviceTelemetry.WebService.Controllers;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using Message = Mmm.Iot.Common.Services.External.TimeSeries.Message;

namespace Mmm.Iot.DeviceTelemetry.WebService.Test.Controllers
{
    public class MessagesControllerTest : IDisposable
    {
        private readonly Mock<ILogger<MessagesController>> logger;
        private readonly MessagesController controller;
        private readonly Mock<IMessages> messageServiceMock;
        private bool disposedValue = false;

        public MessagesControllerTest()
        {
            this.logger = new Mock<ILogger<MessagesController>>();
            this.messageServiceMock = new Mock<IMessages>();
            var mockConfig = new Mock<AppConfig>();
            this.controller = new MessagesController(this.messageServiceMock.Object, this.logger.Object, mockConfig.Object);
        }

        [Fact]
        public async Task GetRecentDeviceMessagesFromTelemetryCollection()
        {
            // Arrange
            int limit = 2;
            const string deviceId = "device1";

            this.messageServiceMock.Setup(x => x.ListTopDeviceMessagesAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(this.CreateFakedeviceMessagesList);

            // Act
            var result = await this.controller.GetTopDeviceMessagesAsync(limit, deviceId);

            // Assert
            this.messageServiceMock
                .Verify(x => x.ListTopDeviceMessagesAsync(limit, deviceId), Times.Once);
            Assert.NotNull(result);

            Assert.Equal(limit, result.Items.Count);
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.controller.Dispose();
                }

                this.disposedValue = true;
            }
        }

        private MessageList CreateFakedeviceMessagesList()
        {
            var sampleMessages = new List<Message>();
            var sampleProperties = new List<string>();

            var data = new JObject
            {
                { "data.sample_unit", "mph" },
                { "data.sample_speed", "10" },
            };

            sampleMessages.Add(new Message("id1", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), data));
            sampleMessages.Add(new Message("id2", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), data));

            sampleProperties.Add("data.sample_unit");
            sampleProperties.Add("data.sample_speed");

            return new MessageList(sampleMessages, sampleProperties);
        }
    }
}