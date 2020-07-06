// <copyright file="DeviceFilesControllerTest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services;
using Mmm.Iot.DeviceTelemetry.Services;
using Mmm.Iot.DeviceTelemetry.Services.Models;
using Mmm.Iot.DeviceTelemetry.WebService.Controllers;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Mmm.Iot.DeviceTelemetry.WebService.Test.Controllers
{
    public class DeviceFilesControllerTest : IDisposable
    {
        private const string TenantId = "TenantId";
        private const string DeviceId = "Device1";
        private readonly Mock<ILogger<MessagesController>> logger;
        private readonly DeviceFilesController controller;
        private readonly Mock<IDeviceFileUploads> fileUploadServiceMock;
        private Mock<HttpContext> mockHttpContext;
        private Mock<HttpRequest> mockHttpRequest;
        private IDictionary<object, object> contextItems;
        private bool disposedValue = false;

        public DeviceFilesControllerTest()
        {
            this.logger = new Mock<ILogger<MessagesController>>();
            this.fileUploadServiceMock = new Mock<IDeviceFileUploads>();
            this.mockHttpContext = new Mock<HttpContext> { DefaultValue = DefaultValue.Mock };
            this.mockHttpRequest = new Mock<HttpRequest> { DefaultValue = DefaultValue.Mock };
            this.mockHttpRequest.Setup(m => m.HttpContext).Returns(this.mockHttpContext.Object);
            this.mockHttpContext.Setup(m => m.Request).Returns(this.mockHttpRequest.Object);
            this.contextItems = new Dictionary<object, object>
            {
                {
                    RequestExtension.ContextKeyTenantId, TenantId
                },
            };
            this.mockHttpContext.Setup(m => m.Items).Returns(this.contextItems);
            this.controller = new DeviceFilesController(this.fileUploadServiceMock.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = this.mockHttpContext.Object,
                },
            };
        }

        [Fact]
        public async Task GetDeviceUploadsTest()
        {
            // Arrange
            IEnumerable<DeviceUpload> deviceUploads = this.FormDeviceUploadList();

            this.fileUploadServiceMock.Setup(x => x.GetDeviceUploads(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(deviceUploads);

            // Act
            var result = await this.controller.GetDeviceUploads(DeviceId);

            // Assert
            this.fileUploadServiceMock
                .Verify(x => x.GetDeviceUploads(TenantId, DeviceId), Times.Once);
            Assert.NotNull(result);
            Assert.Equal(deviceUploads.Count(), result.Items.Count);
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

        private IEnumerable<DeviceUpload> FormDeviceUploadList()
        {
            List<DeviceUpload> deviceUploads = new List<DeviceUpload>();

            deviceUploads.Add(new DeviceUpload
            {
                BlobName = "2020/06/10/Telemetry.zip",
                Size = 200,
                UploadedOn = DateTime.Now.ToString(),
                UploadedBy = DeviceId,
            });
            deviceUploads.Add(new DeviceUpload
            {
                BlobName = "2020/06/11/Telemetry.zip",
                Size = 200,
                UploadedOn = DateTime.Now.ToString(),
                UploadedBy = DeviceId,
            });
            deviceUploads.Add(new DeviceUpload
            {
                BlobName = "2020/06/12/Telemetry.zip",
                Size = 200,
                UploadedOn = DateTime.Now.ToString(),
                UploadedBy = DeviceId,
            });

            return deviceUploads;
        }
    }
}