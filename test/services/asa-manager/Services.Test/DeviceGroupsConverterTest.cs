// <copyright file="DeviceGroupsConverterTest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mmm.Iot.AsaManager.Services;
using Mmm.Iot.AsaManager.Services.Exceptions;
using Mmm.Iot.AsaManager.Services.External.IotHubManager;
using Mmm.Iot.AsaManager.Services.Models;
using Mmm.Iot.AsaManager.Services.Models.DeviceGroups;
using Mmm.Iot.AsaManager.Services.Test.Helpers;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.BlobStorage;
using Mmm.Iot.Common.Services.External.StorageAdapter;
using Mmm.Iot.Common.TestHelpers;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Mmm.Iot.AsaManager.Services.Test
{
    public class DeviceGroupsConverterTest
    {
        private readonly Random rand;
        private Mock<IBlobStorageClient> mockBlobStorageClient;
        private Mock<IStorageAdapterClient> mockStorageAdapterClient;
        private Mock<IIotHubManagerClient> mockIotHubManagerClient;
        private Mock<ILogger<DeviceGroupsConverter>> mockLog;
        private DeviceGroupsConverter converter;
        private CreateEntityHelper entityHelper;

        public DeviceGroupsConverterTest()
        {
            this.mockBlobStorageClient = new Mock<IBlobStorageClient>();
            this.mockStorageAdapterClient = new Mock<IStorageAdapterClient>();
            this.mockIotHubManagerClient = new Mock<IIotHubManagerClient>();
            this.mockLog = new Mock<ILogger<DeviceGroupsConverter>>();
            this.rand = new Random();
            this.entityHelper = new CreateEntityHelper(this.rand);

            this.converter = new DeviceGroupsConverter(
                this.mockIotHubManagerClient.Object,
                this.mockBlobStorageClient.Object,
                this.mockStorageAdapterClient.Object,
                this.mockLog.Object);
        }

        [Fact]
        public async Task ConvertAsyncReturnsExpectedModel()
        {
            string tenantId = this.rand.NextString();
            List<ValueApiModel> deviceGroupsList = new List<ValueApiModel>
            {
                this.entityHelper.CreateDeviceGroup(),
                this.entityHelper.CreateDeviceGroup(),
            };
            ValueListApiModel deviceGroups = new ValueListApiModel
            {
                Items = deviceGroupsList,
            };

            this.mockStorageAdapterClient
                .Setup(c => c.GetAllAsync(
                    It.Is<string>(s => s == this.converter.Entity),
                    It.IsAny<string>()))
                .ReturnsAsync(deviceGroups);

            this.mockBlobStorageClient
                .Setup(c => c.CreateBlobAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            this.mockIotHubManagerClient
                .Setup(c => c.GetListAsync(
                    It.IsAny<IEnumerable<DeviceGroupConditionModel>>(),
                    It.Is<string>(s => s == tenantId),
                    It.IsAny<string>()))
                .ReturnsAsync(new DeviceListModel { Items = new List<DeviceModel> { this.entityHelper.CreateDevice(), this.entityHelper.CreateDevice() } });  // return a device for each device group

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
            this.mockIotHubManagerClient
                .Verify(
                    c => c.GetListAsync(
                        It.IsAny<IEnumerable<DeviceGroupConditionModel>>(),
                        It.Is<string>(s => s == tenantId),
                        It.IsAny<string>()),
                    Times.Exactly(deviceGroups.Items.Count));

            Assert.Equal(conversionResponse.Entities, deviceGroups);
            Assert.Equal(conversionResponse.TenantId, tenantId);
        }

        [Fact]
        public async Task ConvertAsyncThrowsOnEmptyDeviceGroups()
        {
            string tenantId = this.rand.NextString();
            ValueListApiModel deviceGroups = new ValueListApiModel
            {
                Items = new List<ValueApiModel>(),
            };

            this.mockStorageAdapterClient
                .Setup(c => c.GetAllAsync(
                    It.Is<string>(s => s == this.converter.Entity),
                    It.IsAny<string>()))
                .ReturnsAsync(deviceGroups);

            Func<Task> conversion = async () => await this.converter.ConvertAsync(tenantId);

            await Assert.ThrowsAsync<ResourceNotFoundException>(conversion);
        }

        [Fact]
        public async Task ConvertAsyncThrowsOnEmptyDevices()
        {
            string tenantId = this.rand.NextString();
            List<ValueApiModel> deviceGroupsList = new List<ValueApiModel>
            {
                this.entityHelper.CreateDeviceGroup(),
                this.entityHelper.CreateDeviceGroup(),
            };
            ValueListApiModel deviceGroups = new ValueListApiModel
            {
                Items = deviceGroupsList,
            };

            this.mockStorageAdapterClient
                .Setup(c => c.GetAllAsync(
                    It.Is<string>(s => s == this.converter.Entity),
                    It.IsAny<string>()))
                .ReturnsAsync(deviceGroups);

            this.mockIotHubManagerClient
                .Setup(c => c.GetListAsync(
                    It.IsAny<IEnumerable<DeviceGroupConditionModel>>(),
                    It.Is<string>(s => s == tenantId),
                    It.IsAny<string>()))
                .ReturnsAsync(new DeviceListModel { Items = new List<DeviceModel> { } });  // return empty device lists, should cause the exception

            Func<Task> conversion = async () => await this.converter.ConvertAsync(tenantId);

            await Assert.ThrowsAsync<ResourceNotFoundException>(conversion);
        }
    }
}