// <copyright file="DeviceQueryCacheTest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.AppConfiguration;
using Mmm.Iot.Common.Services.External.AsaManager;
using Mmm.Iot.Common.Services.External.CosmosDb;
using Mmm.Iot.Common.TestHelpers;
using Mmm.Iot.IoTHubManager.Services.Models;
using Moq;
using Xunit;

namespace Mmm.Iot.IoTHubManager.Services.Test
{
    public class DeviceQueryCacheTest
    {
        private const string MockTenantId = "mocktenant";

        private readonly Mock<IStorageClient> mockStorage;
        private readonly Mock<IAppConfigurationClient> mockConfig;
        private readonly Mock<ILogger<IDeviceQueryCache>> mockLog;
        private readonly Random random;

        private readonly IDeviceQueryCache deviceQueryCache;

        public DeviceQueryCacheTest()
        {
            this.mockStorage = new Mock<IStorageClient>();

            this.mockConfig = new Mock<IAppConfigurationClient>();
            this.mockConfig
                .Setup(x => x.GetValue(It.IsAny<string>()))
                .Returns(MockTenantId);

            this.mockLog = new Mock<ILogger<IDeviceQueryCache>>();

            this.deviceQueryCache = new DeviceQueryCache(
                this.mockStorage.Object,
                this.mockConfig.Object,
                this.mockLog.Object);

            this.random = new Random();
        }

        [Fact]
        public async Task GetCachedResultAsyncTest()
        {
            var queryString = this.random.NextString();
            var deviceId = this.random.NextString();

            var mockResult = new DeviceServiceListModel(
                new List<DeviceServiceModel>
                {
                    new DeviceServiceModel(
                        null,
                        deviceId,
                        0,
                        new DateTime(DateTimeOffset.Now.ToUnixTimeSeconds()),
                        false,
                        false,
                        false,
                        new DateTime(DateTimeOffset.Now.ToUnixTimeSeconds()),
                        new TwinServiceModel(),
                        new AuthenticationMechanismServiceModel(),
                        this.random.NextString()),
                });

            this.deviceQueryCache.SetTenantQueryResult(
                MockTenantId,
                queryString,
                new DeviceQueryCacheResultServiceModel
                {
                    Result = mockResult,
                    ResultTimestamp = DateTimeOffset.Now,
                });

            this.mockStorage
                .Setup(x => x.QueryDocumentsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<FeedOptions>(),
                    It.IsAny<SqlQuerySpec>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .ReturnsAsync(new List<Document>());

            var result = await this.deviceQueryCache.GetCachedQueryResultAsync(MockTenantId, queryString);

            this.mockStorage
                .Verify(
                    x => x.QueryDocumentsAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<FeedOptions>(),
                        It.IsAny<SqlQuerySpec>(),
                        It.IsAny<int>(),
                        It.IsAny<int>()),
                    Times.Once);

            Assert.Equal(mockResult, result);
        }

        [Fact]
        public async Task GetCachedResultAsyncReturnsNullWhenStorageHasValueTest()
        {
            var queryString = this.random.NextString();
            var deviceId = this.random.NextString();

            var mockResult = new DeviceServiceListModel(
                new List<DeviceServiceModel>
                {
                    new DeviceServiceModel(
                        null,
                        deviceId,
                        0,
                        new DateTime(DateTimeOffset.Now.ToUnixTimeSeconds()),
                        false,
                        false,
                        false,
                        new DateTime(DateTimeOffset.Now.ToUnixTimeSeconds()),
                        new TwinServiceModel(),
                        new AuthenticationMechanismServiceModel(),
                        this.random.NextString()),
                });

            this.deviceQueryCache.SetTenantQueryResult(
                MockTenantId,
                queryString,
                new DeviceQueryCacheResultServiceModel
                {
                    Result = mockResult,
                    ResultTimestamp = DateTimeOffset.Now,
                });

            this.mockStorage
                .Setup(x => x.QueryDocumentsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<FeedOptions>(),
                    It.IsAny<SqlQuerySpec>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .ReturnsAsync(new List<Document>
                    {
                        new Document(),
                    });

            var result = await this.deviceQueryCache.GetCachedQueryResultAsync(MockTenantId, queryString);

            this.mockStorage
                .Verify(
                    x => x.QueryDocumentsAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<FeedOptions>(),
                        It.IsAny<SqlQuerySpec>(),
                        It.IsAny<int>(),
                        It.IsAny<int>()),
                    Times.Once);

            Assert.NotEqual(mockResult, result);
        }

        [Fact]
        public async Task GetCachedResultAsyncReturnsNullWhenCacheDoesNotHaveThatTenantTest()
        {
            var queryString = this.random.NextString();

            var result = await this.deviceQueryCache.GetCachedQueryResultAsync(MockTenantId, queryString);

            this.mockStorage
                .Verify(
                    x => x.QueryDocumentsAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<FeedOptions>(),
                        It.IsAny<SqlQuerySpec>(),
                        It.IsAny<int>(),
                        It.IsAny<int>()),
                    Times.Never);

            Assert.Null(result);
        }
    }
}