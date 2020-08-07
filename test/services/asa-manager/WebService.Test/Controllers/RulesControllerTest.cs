// <copyright file="RulesControllerTest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mmm.Iot.AsaManager.Services;
using Mmm.Iot.AsaManager.Services.External.IotHubManager;
using Mmm.Iot.AsaManager.Services.Models;
using Mmm.Iot.AsaManager.WebService.Controllers;
using Mmm.Iot.AsaManager.WebService.Helpers;
using Mmm.Iot.Common.Services;
using Mmm.Iot.Common.Services.External.BlobStorage;
using Mmm.Iot.Common.Services.External.StorageAdapter;
using Mmm.Iot.Common.Services.Wrappers;
using Mmm.Iot.Common.TestHelpers;
using Moq;
using Xunit;

namespace Mmm.IoT.AsaManager.WebService.Test.Controllers
{
    public class RulesControllerTest : IDisposable
    {
        private const string MockTenantId = "mocktenant";

        private readonly Mock<RulesConverter> mockConverter;
        private readonly Mock<IKeyGenerator> mockGenerator;
        private readonly Mock<BeginConversionHelper> mockHelper;
        private readonly RulesController controller;
        private readonly Random rand = new Random();
        private IDictionary<object, object> contextItems;
        private bool disposedValue = false;

        public RulesControllerTest()
        {
            var mockIotHubClient = new Mock<IIotHubManagerClient>();
            var mockBlobClient = new Mock<IBlobStorageClient>();
            var mockStorageAdapter = new Mock<IStorageAdapterClient>();
            var mockConvertLogger = new Mock<ILogger<RulesConverter>>();
            var mockLogger = new Mock<ILogger<BeginConversionHelper>>();

            this.mockGenerator = new Mock<IKeyGenerator>();

            this.mockConverter = new Mock<RulesConverter>(
                mockBlobClient.Object,
                mockStorageAdapter.Object,
                mockConvertLogger.Object);
            this.mockHelper = new Mock<BeginConversionHelper>(
                mockIotHubClient.Object,
                mockLogger.Object,
                this.mockGenerator.Object);

            var mockHttpContext = new Mock<HttpContext> { DefaultValue = DefaultValue.Mock };
            var mockHttpRequest = new Mock<HttpRequest> { DefaultValue = DefaultValue.Mock };
            mockHttpRequest.Setup(m => m.HttpContext).Returns(mockHttpContext.Object);
            mockHttpContext.Setup(m => m.Request).Returns(mockHttpRequest.Object);

            this.controller = new RulesController(
                this.mockConverter.Object,
                this.mockHelper.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = mockHttpContext.Object,
                },
            };
            this.contextItems = new Dictionary<object, object>
            {
                { RequestExtension.ContextKeyTenantId, MockTenantId },
            };
            mockHttpContext.Setup(m => m.Items).Returns(this.contextItems);
        }

        [Fact]
        public void BeginRuleConversionTest()
        {
            string operationId = this.rand.NextString();

            this.mockGenerator
                .Setup(x => x.Generate())
                .Returns(operationId);

            this.mockConverter
                .Setup(x => x.ConvertAsync(
                    It.Is<string>(s => s == MockTenantId),
                    It.Is<string>(s => s == operationId)))
                .ReturnsAsync(
                    new ConversionApiModel
                    {
                        OperationId = operationId,
                    });

            var response = this.controller.BeginRuleConversion();

            this.mockGenerator
                .Verify(
                    x => x.Generate(),
                    Times.Once);

            this.mockConverter
                .Verify(
                    x => x.ConvertAsync(
                        It.Is<string>(s => s == MockTenantId),
                        It.Is<string>(s => s == operationId)),
                    Times.Once);

            Assert.Equal(MockTenantId, response.TenantId);
            Assert.Equal(operationId, response.OperationId);
        }

        [Fact]
        public void BeginRuleConversionReturnsOnBackgroundExceptionTest()
        {
            string operationId = this.rand.NextString();
            string exceptionMessage = this.rand.NextString();
            Exception convertException = new Exception(exceptionMessage);

            this.mockGenerator
                .Setup(x => x.Generate())
                .Returns(operationId);

            this.mockConverter
                .Setup(x => x.ConvertAsync(
                    It.Is<string>(s => s == MockTenantId),
                    It.Is<string>(s => s == operationId)))
                .ThrowsAsync(convertException);

            var response = this.controller.BeginRuleConversion();
            Thread.Sleep(5000);  // Sleep to allow the ConvertAsync to be called in backgorund

            this.mockConverter
                .Verify(
                    x => x.ConvertAsync(
                        It.Is<string>(s => s == MockTenantId),
                        It.Is<string>(s => s == operationId)),
                    Times.Once);

            Assert.Equal(MockTenantId, response.TenantId);
            Assert.Equal(operationId, response.OperationId);
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
    }
}