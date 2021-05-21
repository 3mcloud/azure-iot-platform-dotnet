// <copyright file="UserTenantControllerTest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services;
using Mmm.Iot.Common.TestHelpers;
using Mmm.Iot.IdentityGateway.Services;
using Mmm.Iot.IdentityGateway.Services.Helpers;
using Mmm.Iot.IdentityGateway.Services.Models;
using Mmm.Iot.IdentityGateway.WebService.Controllers;
using Moq;
using SendGrid;
using SendGrid.Helpers.Mail;
using Xunit;

namespace Mmm.Iot.IdentityGateway.WebService.Test.Controllers
{
    public class UserTenantControllerTest : IDisposable
    {
        private const string SomeUserId = "someUserId";
        private const string SomeSub = "someSub";
        private const string SomeRole = "someRole";
        private const string SomeTenantId = "someTenantId";
        private const string SomeName = "someName";
        private bool disposedValue = false;
        private Mock<UserTenantContainer> mockUserTenantContainer;
        private Mock<UserSettingsContainer> mockUserSettingsContainer;
        private UserTenantController controller;
        private Mock<HttpContext> mockHttpContext;
        private Mock<ILogger<UserTenantContainer>> logger;
        private Mock<ILogger<UserSettingsContainer>> settingsLogger;
        private UserTenantListModel someUserTenantList = new UserTenantListModel();
        private UserTenantModel someUserTenant = new UserTenantModel();
        private UserSettingsListModel someuserSetting = new UserSettingsListModel();
        private Mock<HttpRequest> mockHttpRequest;
        private Mock<IJwtHelpers> mockJwtHelper;
        private Mock<ISendGridClientFactory> mockSendGridClientFactory;
        private Mock<ISendGridClient> mockSendGridClient;
        private Mock<IInviteHelpers> mockInviteHelper;
        private Invitation someInvitation = new Invitation { Role = SomeRole };
        private JwtSecurityToken someSecurityToken;
        private Guid someTenant = Guid.NewGuid();
        private HostString someHost = new HostString("somehost");
        private IDictionary<object, object> contextItems;

        public UserTenantControllerTest()
        {
            this.InitializeController();
            this.SetupDefaultBehaviors();
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task GetAllUsersForTenantReturnsExpectedUserTenantList()
        {
            // Arrange
            // Act
            var result = await this.controller.GetAllUsersForTenantAsync();

            // Assert
            Assert.Equal(this.someUserTenantList, result);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task UserClaimsGetAllTenantsForUserReturnsExpectedUserTenantList()
        {
            // Arrange
            // Act
            var result = await this.controller.UserClaimsGetAllTenantsForUserAsync();

            // Assert
            Assert.Equal(this.someUserTenantList, result);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task GetAllTenantsForUserReturnsExpectedUserTenantList()
        {
            // Arrange
            // Act
            var result = await this.controller.GetAllTenantsForUserAsync(SomeUserId);

            // Assert
            Assert.Equal(this.someUserTenantList, result);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task UserClaimsGetReturnsExpectedUserTenant()
        {
            // Arrange
            // Act
            var result = await this.controller.UserClaimsGetAsync();

            // Assert
            Assert.Equal(this.someUserTenant, result);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task GetReturnsExpectedUserTenant()
        {
            // Arrange
            // Act
            var result = await this.controller.GetAsync(SomeUserId);

            // Assert
            Assert.Equal(this.someUserTenant, result);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task PostReturnsExpectedUserTenant()
        {
            // Arrange
            // Act
            var result = await this.controller.PostAsync(SomeUserId, this.someUserTenant);

            // Assert
            Assert.Equal(this.someUserTenant, result);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task UserClaimsPostReturnsExpectedUserTenant()
        {
            // Arrange
            // Act
            var result = await this.controller.UserClaimsPostAsync(this.someUserTenant);

            // Assert
            Assert.Equal(this.someUserTenant, result);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task PutReturnsExpectedUserTenant()
        {
            // Arrange
            // Act
            var result = await this.controller.PutAsync(SomeUserId, this.someUserTenant);

            // Assert
            Assert.Equal(this.someUserTenant, result);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task UserClaimsPutReturnsExpectedUserTenant()
        {
            // Arrange
            // Act
            var result = await this.controller.UserClaimsPutAsync(this.someUserTenant);

            // Assert
            Assert.Equal(this.someUserTenant, result);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task DeleteReturnsExpectedUserTenant()
        {
            // Arrange
            // Act
            var result = await this.controller.DeleteAsync(SomeUserId);

            // Assert
            Assert.Equal(this.someUserTenant, result);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task UserClaimsDeleteReturnsExpectedUserTenant()
        {
            // Arrange
            // Act
            var result = await this.controller.UserClaimsDeleteAsync();

            // Assert
            Assert.Equal(this.someUserTenant, result);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task DeleteAllReturnsExpectedUserTenantList()
        {
            // Arrange
            // Act
            var result = await this.controller.DeleteAllAsync();

            // Assert
            Assert.Equal(this.someUserTenantList, result);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task InviteReturnsExpectedUserTenant()
        {
            // Arrange
            // Act
            var result = await this.controller.InviteAsync(this.someInvitation);

            // Assert
            Assert.Equal(this.someUserTenant, result);
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

        private void InitializeController()
        {
            this.mockJwtHelper = new Mock<IJwtHelpers> { DefaultValue = DefaultValue.Mock };
            this.logger = new Mock<ILogger<UserTenantContainer>>();
            this.mockUserTenantContainer = new Mock<UserTenantContainer>(this.logger.Object);
            this.settingsLogger = new Mock<ILogger<UserSettingsContainer>>();
            this.mockUserSettingsContainer = new Mock<UserSettingsContainer>(this.settingsLogger.Object);
            this.mockHttpContext = new Mock<HttpContext> { DefaultValue = DefaultValue.Mock };
            this.mockHttpRequest = new Mock<HttpRequest> { DefaultValue = DefaultValue.Mock };
            this.mockSendGridClientFactory = new Mock<ISendGridClientFactory> { DefaultValue = DefaultValue.Mock };
            this.mockSendGridClient = new Mock<ISendGridClient> { DefaultValue = DefaultValue.Mock };
            this.mockInviteHelper = new Mock<IInviteHelpers>();

            this.controller = new UserTenantController(this.mockUserTenantContainer.Object, this.mockJwtHelper.Object, this.mockSendGridClientFactory.Object, this.mockInviteHelper.Object, this.mockUserSettingsContainer.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = this.mockHttpContext.Object,
                },
            };
        }

        private void SetupDefaultBehaviors()
        {
            this.mockUserTenantContainer.Setup(m => m.GetAllAsync(It.IsAny<UserTenantInput>())).ReturnsAsync(this.someUserTenantList);
            this.mockUserTenantContainer.Setup(m => m.GetAllUsersAsync(It.IsAny<UserTenantInput>())).ReturnsAsync(this.someUserTenantList);
            this.mockUserTenantContainer.Setup(m => m.GetAsync(It.IsAny<UserTenantInput>())).ReturnsAsync(this.someUserTenant);
            this.mockUserSettingsContainer.Setup(m => m.GetAllAsync(It.IsAny<UserSettingsInput>())).ReturnsAsync(this.someuserSetting);
            this.mockUserTenantContainer.Setup(m => m.CreateAsync(It.IsAny<UserTenantInput>())).ReturnsAsync(this.someUserTenant);
            this.mockUserTenantContainer.Setup(m => m.UpdateAsync(It.IsAny<UserTenantInput>())).ReturnsAsync(this.someUserTenant);
            this.mockUserTenantContainer.Setup(m => m.DeleteAsync(It.IsAny<UserTenantInput>())).ReturnsAsync(this.someUserTenant);
            this.mockUserTenantContainer.Setup(m => m.DeleteAllAsync(It.IsAny<UserTenantInput>())).ReturnsAsync(this.someUserTenantList);
            this.mockSendGridClientFactory.Setup(m => m.CreateSendGridClient()).Returns(this.mockSendGridClient.Object);
            this.someSecurityToken = new JwtSecurityToken(null, null, new List<Claim> { new Claim("available_tenants", this.someTenant.ToString()) });
            this.mockJwtHelper.Setup(m => m.MintToken(It.IsAny<List<Claim>>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns(this.someSecurityToken);
            this.mockSendGridClient.Setup(m => m.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Response(HttpStatusCode.OK, new StringContent(string.Empty), null));
            this.mockHttpRequest.Setup(m => m.HttpContext).Returns(this.mockHttpContext.Object);
            this.mockHttpRequest.Setup(m => m.Host).Returns(this.someHost);
            this.mockHttpContext.Setup(m => m.Request).Returns(this.mockHttpRequest.Object);
            this.contextItems = new Dictionary<object, object>
            {
                {
                    RequestExtension.ContextKeyUserClaims,
                    new List<Claim>
                    {
                        new Claim(RequestExtension.UserObjectIdClaimType, SomeSub),
                        new Claim(RequestExtension.ClaimKeyUserName, SomeName),
                    }
                },
                {
                    RequestExtension.ContextKeyTenantId, SomeTenantId
                },
            };
            this.mockHttpContext.Setup(m => m.Items).Returns(this.contextItems);
        }
    }
}