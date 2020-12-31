// <copyright file="DeviceGroupsConfigClientTest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Helpers;
using Mmm.Iot.Common.TestHelpers;
using Mmm.Iot.TenantManager.Services.External;
using Mmm.Iot.TenantManager.Services.Models;
using Moq;
using TestStack.Dossier;
using TestStack.Dossier.EquivalenceClasses;
using Xunit;

namespace Mmm.Iot.TenantManager.Services.Test
{
    public class DeviceGroupsConfigClientTest
    {
        private readonly Mock<ILogger<DeviceGroupsConfigClient>> logger;
        private readonly Mock<IExternalRequestHelper> mockExternalRequestHelper;
        private readonly Mock<AppConfig> mockAppConfig;
        private readonly AnonymousValueFixture any;
        private readonly string tenantId;
        private DeviceGroupsConfigClient deviceGroupsConfigClient;

        private Random random = new Random();

        public DeviceGroupsConfigClientTest()
        {
            this.logger = new Mock<ILogger<DeviceGroupsConfigClient>>();
            this.mockExternalRequestHelper = new Mock<IExternalRequestHelper> { DefaultValue = DefaultValue.Mock };
            this.mockAppConfig = new Mock<AppConfig> { DefaultValue = DefaultValue.Mock };
            this.any = new AnonymousValueFixture();
            this.tenantId = this.any.String();
            this.deviceGroupsConfigClient = new DeviceGroupsConfigClient(this.mockAppConfig.Object, this.mockExternalRequestHelper.Object);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async void CreateDefaultDeviceGroupAsyncCreatesDefaultDeviceGroup()
        {
            // Arrange
            var defaultDeviceGroupDisplayName = this.any.String();
            this.mockExternalRequestHelper.Setup(m => m.ProcessRequestAsync(It.IsAny<HttpMethod>(), It.IsAny<string>(), It.IsAny<DeviceGroupApiModel>(), this.tenantId, null)).ReturnsAsync(new DeviceGroupApiModel { DisplayName = defaultDeviceGroupDisplayName });

            // Act
            var result = await this.deviceGroupsConfigClient.CreateDefaultDeviceGroupAsync(this.tenantId);

            // Assert
            Assert.Equal(defaultDeviceGroupDisplayName, result.DisplayName);
        }
    }
}