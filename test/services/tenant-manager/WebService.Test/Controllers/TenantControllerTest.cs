// <copyright file="TenantControllerTest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services;
using Mmm.Iot.TenantManager.Services;
using Mmm.Iot.TenantManager.Services.Models;
using Mmm.Iot.TenantManager.WebService.Controllers;
using Moq;
using Xunit;

namespace Mmm.Iot.TenantManager.WebService.Test.Controllers
{
    public class TenantControllerTest : IDisposable
    {
        private const string TenantId = "TenantId";
        private const string CurrentUserClaims = "CurrentUser Claims";
        private const string TenantName = "TenantName";
        private readonly Mock<ILogger<TenantController>> mockLogger;
        private readonly Mock<ITenantContainer> mockTenantContainer;
        private readonly List<Claim> claims;
        private readonly TenantController controller;
        private Mock<HttpContext> mockHttpContext;
        private Mock<HttpRequest> mockHttpRequest;
        private IDictionary<object, object> contextItems;
        private bool disposedValue = false;

        public TenantControllerTest()
        {
            this.mockLogger = new Mock<ILogger<TenantController>>();
            this.mockTenantContainer = new Mock<ITenantContainer>();
            this.mockHttpContext = new Mock<HttpContext> { DefaultValue = DefaultValue.Mock };
            this.mockHttpRequest = new Mock<HttpRequest> { DefaultValue = DefaultValue.Mock };
            this.mockHttpRequest.Setup(m => m.HttpContext).Returns(this.mockHttpContext.Object);
            this.mockHttpContext.Setup(m => m.Request).Returns(this.mockHttpRequest.Object);
            this.claims = new List<Claim>();
            this.claims.Add(new Claim("sub", "Admin"));
            this.claims.Add(new Claim("name", "Admin"));
            this.controller = new TenantController(this.mockTenantContainer.Object, this.mockLogger.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = this.mockHttpContext.Object,
                },
            };
            this.contextItems = new Dictionary<object, object>
            {
                {
                    RequestExtension.ContextKeyTenantId, TenantId
                },
                {
                    RequestExtension.ContextKeyUserClaims, this.claims
                },
            };
            this.mockHttpContext.Setup(m => m.Items).Returns(this.contextItems);
        }

        [Fact]
        public async Task StartAsyncTest()
        {
            // Arrange
            TenantModel tenantModel = new TenantModel()
            {
                ETag = "ETag",
                IotHubName = "IotHubName",
                IsIotHubDeployed = true,
                PartitionKey = "PartitionKey",
                RowKey = "RowKey",
                SAJobName = "SAJobName",
                TenantId = TenantId,
                Timestamp = DateTime.Now,
            };

            this.mockTenantContainer.Setup(x => x.GetTenantAsync(It.IsAny<string>()))
                                            .ReturnsAsync(tenantModel);

            // Act
            var result = await this.controller.GetAsync();

            // Assert
            Assert.True(result.IsIotHubDeployed);
            Assert.Equal(result.TenantId, TenantId);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PostAsyncTest(bool expectException)
        {
            // Arrange
            CreateTenantModel tenantModel = new CreateTenantModel(TenantId);
            CreateTenantModel result;

            if (expectException)
            {
                this.mockTenantContainer.Setup(x => x.CreateTenantAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                                                .ThrowsAsync(new Exception());
            }
            else
            {
                this.mockTenantContainer.Setup(x => x.CreateTenantAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                                                .ReturnsAsync(tenantModel);
            }

            try
            {
                // Act
                result = await this.controller.PostAsync();

                // Assert
                Assert.False(expectException);
            }
            catch (Exception)
            {
                Assert.True(expectException);
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public async Task DeleteAsyncTest(bool ensureFullyDeployed, bool expectCreateNew)
        {
            // Arrange
            var deletionRecord = new Dictionary<string, bool>
            {
                { "IotHub", true },
                { "DPS", true },
            };

            DeleteTenantModel deleteModel = new DeleteTenantModel(TenantId, deletionRecord, ensureFullyDeployed);

            this.mockTenantContainer.Setup(x => x.DeleteTenantAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                                            .ReturnsAsync(deleteModel);

            // Act
            var result = await this.controller.DeleteAsync(ensureFullyDeployed);

            // Assert
            if (ensureFullyDeployed)
            {
                Assert.True(expectCreateNew);
            }
            else
            {
                Assert.False(expectCreateNew);
            }

            Assert.True(result.FullyDeleted);
        }

        [Fact]
        public async Task GetAllTenantAsyncTest()
        {
            // Arrange
            UserTenantListModel userTenants = new UserTenantListModel("GetAllTenants", new List<UserTenantModel>());

            this.mockTenantContainer.Setup(x => x.GetAllTenantsAsync(It.IsAny<string>()))
                                            .ReturnsAsync(userTenants);

            // Act
            var result = await this.controller.GetTenantsAsync();

            // Assert
            Assert.Equal(result.BatchMethod, userTenants.BatchMethod);
            Assert.Equal(result.Models, userTenants.Models);
        }

        [Theory]
        [InlineData(TenantId, TenantName)]
        public async Task UpdateAsyncTest(string tenantId, string tenantName)
        {
            // Arrange
            TenantModel tenantModel = new TenantModel()
            {
                ETag = "ETag",
                IotHubName = "IotHubName",
                IsIotHubDeployed = true,
                PartitionKey = "PartitionKey",
                RowKey = "RowKey",
                SAJobName = "SAJobName",
                TenantId = TenantId,
                Timestamp = DateTime.Now,
                TenantName = tenantName,
            };

            this.mockTenantContainer.Setup(x => x.UpdateTenantAsync(It.IsAny<string>(), It.IsAny<string>()))
                                            .ReturnsAsync(tenantModel);

            // Act
            var result = await this.controller.UpdateAsync(tenantId, tenantName);

            // Assert
            Assert.Equal(result.TenantName, TenantName);
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