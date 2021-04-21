// <copyright file="SystemAdminControllerTest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
using Xunit;

namespace Mmm.Iot.IdentityGateway.WebService.Test.Controllers
{
    public class SystemAdminControllerTest : IDisposable
    {
        private const string SomeUserId = "someUserId";
        private const string SomeSub = "someSub";
        private const string SomeName = "someName";
        private const string SomeSetting = "someSetting";
        private const string SomeValue = "someValue";
        private bool disposedValue = false;
        private Mock<SystemAdminContainer> mockSystemAdminContainer;
        private Mock<UserTenantContainer> mockUserTenantContainer;
        private SystemAdminController controller;
        private Mock<HttpContext> mockHttpContext;
        private Mock<ILogger<SystemAdminContainer>> logger;
        private Mock<ILogger<UserTenantContainer>> userLogger;
        private Mock<HttpRequest> mockHttpRequest;
        private SystemAdminModel someSystemAdmin = new SystemAdminModel();
        private SystemAdminListModel someSystemAdminList = new SystemAdminListModel();
        private UserListModel someUserListModel = new UserListModel();
        private UserModel someUser = new UserModel();
        private IDictionary<object, object> contextItems;
        private TenantListModel someTenantListModel = new TenantListModel();
        private UserTenantListModel someUserTenantListModel = new UserTenantListModel();

        public SystemAdminControllerTest()
        {
            this.InitializeController();
            this.SetupDefaultBehaviors();
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task CreateSystemAdminAsyncReturnsExpectedUser()
        {
            // Arrange
            // Act
            var result = await this.controller.CreateSystemAdminAsync(this.someUser);

            // Assert
            Assert.Equal(this.someSystemAdmin, result);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task GetAllNonSystemAdminsAsyncReturnsExpectedUserList()
        {
            // Arrange
            // Act
            var result = await this.controller.GetAllNonSystemAdminsAsync();

            // Assert
            Assert.Equal(this.someUserListModel, result);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task GetAllSystemAdminsAsyncReturnsExpectedUsersList()
        {
            this.someSystemAdminList.Models = new List<SystemAdminModel>();
            var result = await this.controller.GetAllSystemAdminsAsync();

            Assert.Equal(this.someSystemAdminList.Models.Count(), result.Models.Count());
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task DeleteAsyncResultsModel()
        {
            var result = await this.controller.DeleteSystemAdminAsync(string.Empty);

            Assert.Equal(this.someSystemAdmin, result);
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
            this.logger = new Mock<ILogger<SystemAdminContainer>>();
            this.userLogger = new Mock<ILogger<UserTenantContainer>>();
            this.mockSystemAdminContainer = new Mock<SystemAdminContainer>(this.logger.Object);
            this.mockUserTenantContainer = new Mock<UserTenantContainer>(this.userLogger.Object);
            this.mockHttpContext = new Mock<HttpContext> { DefaultValue = DefaultValue.Mock };
            this.mockHttpRequest = new Mock<HttpRequest> { DefaultValue = DefaultValue.Mock };
            this.controller = new SystemAdminController(this.mockSystemAdminContainer.Object, this.mockUserTenantContainer.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = this.mockHttpContext.Object,
                },
            };
        }

        private void SetupDefaultBehaviors()
        {
            this.mockSystemAdminContainer.Setup(m => m.CreateAsync(It.IsAny<SystemAdminInput>())).ReturnsAsync(this.someSystemAdmin);
            this.mockSystemAdminContainer.Setup(m => m.GetAllAsync()).ReturnsAsync(this.someSystemAdminList);
            this.mockSystemAdminContainer.Setup(m => m.DeleteAsync(It.IsAny<SystemAdminInput>())).ReturnsAsync(this.someSystemAdmin);
            this.mockUserTenantContainer.Setup(m => m.GetAllUsersAsync()).ReturnsAsync(this.someUserListModel);
            this.mockUserTenantContainer.Setup(m => m.GetAllActiveTenantAsync()).ReturnsAsync(this.someTenantListModel);
            this.mockUserTenantContainer.Setup(m => m.GetAllAsync(It.IsAny<UserTenantInput>())).ReturnsAsync(this.someUserTenantListModel);
            this.mockHttpRequest.Setup(m => m.HttpContext).Returns(this.mockHttpContext.Object);
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
            };
            this.mockHttpContext.Setup(m => m.Items).Returns(this.contextItems);
        }
    }
}