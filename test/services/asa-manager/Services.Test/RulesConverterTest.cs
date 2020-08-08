// <copyright file="RulesConverterTest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mmm.Iot.AsaManager.Services.Models;
using Mmm.Iot.AsaManager.Services.Test.Helpers;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.BlobStorage;
using Mmm.Iot.Common.Services.External.StorageAdapter;
using Mmm.Iot.Common.TestHelpers;
using Moq;
using Xunit;

namespace Mmm.Iot.AsaManager.Services.Test
{
    public class RulesConverterTest
    {
        private readonly Random rand;
        private Mock<IBlobStorageClient> mockBlobStorageClient;
        private Mock<IStorageAdapterClient> mockStorageAdapterClient;
        private Mock<ILogger<RulesConverter>> mockLog;
        private RulesConverter converter;
        private CreateEntityHelper entityHelper;

        public RulesConverterTest()
        {
            this.mockBlobStorageClient = new Mock<IBlobStorageClient>();
            this.mockStorageAdapterClient = new Mock<IStorageAdapterClient>();
            this.mockLog = new Mock<ILogger<RulesConverter>>();
            this.rand = new Random();
            this.entityHelper = new CreateEntityHelper(this.rand);

            this.converter = new RulesConverter(
                this.mockBlobStorageClient.Object,
                this.mockStorageAdapterClient.Object,
                this.mockLog.Object);
        }

        [Fact]
        public async Task ConvertAsyncReturnsExpectedModel()
        {
            string tenantId = this.rand.NextString();
            List<ValueApiModel> rulesList = new List<ValueApiModel>
            {
                this.entityHelper.CreateRule(),
                this.entityHelper.CreateRule(),
            };
            ValueListApiModel rules = new ValueListApiModel
            {
                Items = rulesList,
            };

            this.mockStorageAdapterClient
                .Setup(c => c.GetAllAsync(
                    It.Is<string>(s => s == this.converter.Entity),
                    It.IsAny<string>()))
                .ReturnsAsync(rules);

            this.mockBlobStorageClient
                .Setup(c => c.CreateBlobAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            ConversionApiModel conversionResponse = await this.converter.ConvertAsync(tenantId);

            this.mockStorageAdapterClient
                .Verify(
                    c => c.GetAllAsync(
                        It.Is<string>(s => s == this.converter.Entity),
                        It.IsAny<string>()),
                    Times.Once);
            this.mockBlobStorageClient
                .Verify(
                    c => c.CreateBlobAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()),
                    Times.Once);

            Assert.Equal(conversionResponse.Entities, rules);
            Assert.Equal(conversionResponse.TenantId, tenantId);
        }

        [Fact]
        public async Task ConvertAsyncThrowsOnEmptyRules()
        {
            string tenantId = this.rand.NextString();
            ValueListApiModel rules = new ValueListApiModel
            {
                Items = new List<ValueApiModel>(),
            };

            this.mockStorageAdapterClient
                .Setup(c => c.GetAllAsync(
                    It.Is<string>(s => s == this.converter.Entity),
                    It.IsAny<string>()))
                .ReturnsAsync(rules);

            Func<Task> conversion = async () => await this.converter.ConvertAsync(tenantId);

            await Assert.ThrowsAsync<ResourceNotFoundException>(conversion);
        }
    }
}