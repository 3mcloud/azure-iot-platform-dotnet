// <copyright file="IdentityGatewayClientTests.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Helpers;
using Mmm.Iot.Common.TestHelpers;
using Mmm.Iot.TenantManager.Services.External;
using Mmm.Iot.TenantManager.Services.Models;
using Moq;
using Newtonsoft.Json;
using TestStack.Dossier;
using TestStack.Dossier.EquivalenceClasses;
using Xunit;

namespace Mmm.Iot.TenantManager.Services.Test
{
    public class IdentityGatewayClientTests
    {
        private readonly Mock<ILogger<IdentityGatewayClient>> logger;
        private readonly Mock<IExternalRequestHelper> mockExternalRequestHelper;
        private readonly Mock<AppConfig> mockAppConfig;
        private readonly AnonymousValueFixture any;
        private readonly string tenantId;
        private readonly string userId;

        private readonly string roles;
        private readonly string value;
        private IdentityGatewayClient identityGatewayClient;

        private Random random = new Random();

        public IdentityGatewayClientTests()
        {
            this.logger = new Mock<ILogger<IdentityGatewayClient>>();
            this.mockExternalRequestHelper = new Mock<IExternalRequestHelper> { DefaultValue = DefaultValue.Mock };
            this.mockAppConfig = new Mock<AppConfig> { DefaultValue = DefaultValue.Mock };
            this.any = new AnonymousValueFixture();
            this.tenantId = this.any.String();
            this.userId = this.any.String();
            this.roles = this.any.String();
            this.value = this.any.String();
            this.identityGatewayClient = new IdentityGatewayClient(this.mockAppConfig.Object, this.mockExternalRequestHelper.Object);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async void GetTenantForUserAsync()
        {
            // Arrange
            IdentityGatewayApiModel model = new IdentityGatewayApiModel { TenantId = this.tenantId, Roles = this.roles, UserId = this.userId };
            this.mockExternalRequestHelper.Setup(m => m.ProcessRequestAsync<IdentityGatewayApiModel>(It.IsAny<HttpMethod>(), It.IsAny<string>(), this.tenantId, null)).ReturnsAsync(model);

            // Act
            IdentityGatewayApiModel result = await this.identityGatewayClient.GetTenantForUserAsync(this.userId, this.tenantId);

            // Assert
            Assert.Equal(model.TenantId, result.TenantId);
            Assert.Equal(model.Roles, result.Roles);
            Assert.Equal(model.UserId, result.UserId);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async void AddTenantForUserAsync()
        {
            // Arrange
            IdentityGatewayApiModel model = new IdentityGatewayApiModel { TenantId = this.tenantId, Roles = this.roles, UserId = this.userId };
            this.mockExternalRequestHelper.Setup(m => m.ProcessRequestAsync<IdentityGatewayApiModel>(It.IsAny<HttpMethod>(), It.IsAny<string>(), It.IsAny<IdentityGatewayApiModel>(), this.tenantId, null)).ReturnsAsync(model);

            // Act
            var result = await this.identityGatewayClient.AddTenantForUserAsync(this.userId, this.tenantId, this.roles);

            // Assert
            Assert.Equal(model.TenantId, result.TenantId);
            Assert.Equal(model.Roles, result.Roles);
            Assert.Equal(model.UserId, result.UserId);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async void UpdateSettingsForUserAsync()
        {
            // Arrange
            IdentityGatewayApiSettingModel model = new IdentityGatewayApiSettingModel { Value = this.value, UserId = this.userId };
            this.mockExternalRequestHelper.Setup(m => m.ProcessRequestAsync<IdentityGatewayApiSettingModel>(It.IsAny<HttpMethod>(), It.IsAny<string>(), null, null)).ReturnsAsync(model);

            // Act
            var result = await this.identityGatewayClient.UpdateSettingsForUserAsync(this.userId, this.tenantId, this.value);

            // Assert
            Assert.Equal(model.UserId, result.UserId);
            Assert.Equal(model.Value, result.Value);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async void AddSettingsForUserAsync()
        {
            // Arrange
            IdentityGatewayApiSettingModel model = new IdentityGatewayApiSettingModel { Value = this.value, UserId = this.userId };
            this.mockExternalRequestHelper.Setup(m => m.ProcessRequestAsync<IdentityGatewayApiSettingModel>(It.IsAny<HttpMethod>(), It.IsAny<string>(), null, null)).ReturnsAsync(model);

            // Act
            var result = await this.identityGatewayClient.AddSettingsForUserAsync(this.userId, this.tenantId, this.value);

            // Assert
            Assert.Equal(model.UserId, result.UserId);
            Assert.Equal(model.Value, result.Value);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async void DeleteTenantForAllUsersAsync()
        {
            // Arrange
            IdentityGatewayApiModel model = new IdentityGatewayApiModel { };
            this.mockExternalRequestHelper.Setup(m => m.ProcessRequestAsync<IdentityGatewayApiModel>(It.IsAny<HttpMethod>(), It.IsAny<string>(), this.tenantId, null)).ReturnsAsync(model);

            // Act
            var result = await this.identityGatewayClient.DeleteTenantForAllUsersAsync(this.tenantId);

            // Assert
            Assert.Null(result.TenantId);
            Assert.Null(result.Roles);
            Assert.Null(result.UserId);
        }
    }
}