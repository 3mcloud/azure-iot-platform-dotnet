// <copyright file="DeviceGroupControllerTest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.Common.TestHelpers;
using Mmm.Iot.Config.Services;
using Mmm.Iot.Config.Services.Models;
using Mmm.Iot.Config.WebService.Controllers;
using Mmm.Iot.Config.WebService.Models;
using Moq;
using Xunit;
using DeviceGroup = Mmm.Iot.Config.Services.Models.DeviceGroup;
using DeviceGroupCondition = Mmm.Iot.Config.Services.Models.DeviceGroupCondition;

namespace Mmm.Iot.Config.WebService.Test.Controllers
{
    public class DeviceGroupControllerTest : IDisposable
    {
        private readonly Mock<IStorage> mockStorage;
        private readonly DeviceGroupController controller;
        private readonly Random rand;
        private bool disposedValue = false;

        public DeviceGroupControllerTest()
        {
            this.mockStorage = new Mock<IStorage>();
            this.controller = new DeviceGroupController(this.mockStorage.Object);
            this.rand = new Random();
        }

        [Fact]
        public async Task GetAllAsyncTest()
        {
            var models = new[]
            {
                new DeviceGroup
                {
                    Id = this.rand.NextString(),
                    DisplayName = this.rand.NextString(),
                    Conditions = new List<DeviceGroupCondition>()
                    {
                        new DeviceGroupCondition()
                        {
                            Key = this.rand.NextString(),
                            Operator = DeviceGroupConditionOperatorType.EQ,
                            Value = this.rand.NextString(),
                        },
                    },
                    ETag = this.rand.NextString(),
                },
                new DeviceGroup
                {
                    Id = this.rand.NextString(),
                    DisplayName = this.rand.NextString(),
                    Conditions = new List<DeviceGroupCondition>()
                    {
                        new DeviceGroupCondition()
                        {
                            Key = this.rand.NextString(),
                            Operator = DeviceGroupConditionOperatorType.EQ,
                            Value = this.rand.NextString(),
                        },
                    },
                    ETag = this.rand.NextString(),
                },
                new DeviceGroup
                {
                    Id = this.rand.NextString(),
                    DisplayName = this.rand.NextString(),
                    Conditions = new List<DeviceGroupCondition>()
                    {
                        new DeviceGroupCondition()
                        {
                            Key = this.rand.NextString(),
                            Operator = DeviceGroupConditionOperatorType.EQ,
                            Value = this.rand.NextString(),
                        },
                    },
                    ETag = this.rand.NextString(),
                },
            };

            this.mockStorage
                .Setup(x => x.GetAllDeviceGroupsAsync())
                .ReturnsAsync(models);

            var result = await this.controller.GetAllAsync();

            this.mockStorage
                .Verify(x => x.GetAllDeviceGroupsAsync(), Times.Once);

            Assert.Equal(result.Items.Count(), models.Length);
            foreach (var item in result.Items)
            {
                var model = models.Single(g => g.Id == item.Id);
                Assert.Equal(model.DisplayName, item.DisplayName);
                Assert.Equal(model.Conditions, item.Conditions);
                Assert.Equal(model.ETag, item.ETag);
            }
        }

        [Fact]
        public async Task GetAsyncTest()
        {
            var groupId = this.rand.NextString();
            var displayName = this.rand.NextString();
            var conditions = new List<DeviceGroupCondition>()
            {
                new DeviceGroupCondition()
                {
                    Key = this.rand.NextString(),
                    Operator = DeviceGroupConditionOperatorType.EQ,
                    Value = this.rand.NextString(),
                },
            };
            var etag = this.rand.NextString();

            this.mockStorage
                .Setup(x => x.GetDeviceGroupAsync(It.IsAny<string>()))
                .ReturnsAsync(new DeviceGroup
                {
                    Id = groupId,
                    DisplayName = displayName,
                    Conditions = conditions,
                    ETag = etag,
                });

            var result = await this.controller.GetAsync(groupId);

            this.mockStorage
                .Verify(
                    x => x.GetDeviceGroupAsync(
                        It.Is<string>(s => s == groupId)),
                    Times.Once);

            Assert.Equal(result.DisplayName, displayName);
            Assert.Equal(result.Conditions, conditions);
            Assert.Equal(result.ETag, etag);
        }

        [Fact]
        public async Task CreateAsyncTest()
        {
            var groupId = this.rand.NextString();
            var displayName = this.rand.NextString();
            var conditions = new List<DeviceGroupCondition>()
            {
                new DeviceGroupCondition()
                {
                    Key = this.rand.NextString(),
                    Operator = DeviceGroupConditionOperatorType.EQ,
                    Value = this.rand.NextString(),
                },
            };
            var etag = this.rand.NextString();

            this.mockStorage
                .Setup(x => x.CreateDeviceGroupAsync(It.IsAny<DeviceGroup>()))
                .ReturnsAsync(new DeviceGroup
                {
                    Id = groupId,
                    DisplayName = displayName,
                    Conditions = conditions,
                    ETag = etag,
                });

            var result = await this.controller.CreateAsync(new DeviceGroupApiModel
            {
                DisplayName = displayName,
                Conditions = conditions,
            });

            this.mockStorage
                .Verify(
                    x => x.CreateDeviceGroupAsync(
                        It.Is<DeviceGroup>(m => m.DisplayName == displayName && m.Conditions.First() == conditions.First())),
                    Times.Once);

            Assert.Equal(result.Id, groupId);
            Assert.Equal(result.DisplayName, displayName);
            Assert.Equal(result.Conditions, conditions);
            Assert.Equal(result.ETag, etag);
        }

        [Fact]
        public async Task CreateAsyncThrowsConflictingResource()
        {
            this.mockStorage
                .Setup(x => x.CreateDeviceGroupAsync(
                    It.IsAny<DeviceGroup>()))
                .ThrowsAsync(new ConflictingResourceException());

            var group = new DeviceGroup
            {
                Id = this.rand.NextString(),
                DisplayName = this.rand.NextString(),
                Conditions = new List<DeviceGroupCondition>(),
            };

            var model = new DeviceGroupApiModel(group);

            await Assert.ThrowsAsync<ConflictingResourceException>(async () =>
                await this.controller.CreateAsync(model));
        }

        [Fact]
        public async Task UpdateAsyncTest()
        {
            var groupId = this.rand.NextString();
            var displayName = this.rand.NextString();
            var conditions = new List<DeviceGroupCondition>()
            {
                new DeviceGroupCondition()
                {
                    Key = this.rand.NextString(),
                    Operator = DeviceGroupConditionOperatorType.EQ,
                    Value = this.rand.NextString(),
                },
            };
            var etagOld = this.rand.NextString();
            var etagNew = this.rand.NextString();

            this.mockStorage
                .Setup(x => x.UpdateDeviceGroupAsync(It.IsAny<string>(), It.IsAny<DeviceGroup>(), It.IsAny<string>()))
                .ReturnsAsync(new DeviceGroup
                {
                    Id = groupId,
                    DisplayName = displayName,
                    Conditions = conditions,
                    ETag = etagNew,
                });

            var result = await this.controller.UpdateAsync(
                groupId,
                new DeviceGroupApiModel
                {
                    DisplayName = displayName,
                    Conditions = conditions,
                    ETag = etagOld,
                });

            this.mockStorage
                .Verify(
                    x => x.UpdateDeviceGroupAsync(
                        It.Is<string>(s => s == groupId),
                        It.Is<DeviceGroup>(m => m.DisplayName == displayName && m.Conditions.First() == conditions.First()),
                        It.Is<string>(s => s == etagOld)),
                    Times.Once);

            Assert.Equal(result.Id, groupId);
            Assert.Equal(result.DisplayName, displayName);
            Assert.Equal(result.Conditions, conditions);
            Assert.Equal(result.ETag, etagNew);
        }

        [Fact]
        public async Task UpdateAsyncThrowsConflictingResource()
        {
            this.mockStorage
                .Setup(x => x.UpdateDeviceGroupAsync(
                    It.IsAny<string>(),
                    It.IsAny<DeviceGroup>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new ConflictingResourceException());

            var group = new DeviceGroup
            {
                Id = this.rand.NextString(),
                DisplayName = this.rand.NextString(),
                Conditions = new List<DeviceGroupCondition>(),
            };

            var model = new DeviceGroupApiModel(group);

            await Assert.ThrowsAsync<ConflictingResourceException>(async () =>
                await this.controller.UpdateAsync(model.Id, model));
        }

        [Fact]
        public async Task DeleteAsyncTest()
        {
            var groupId = this.rand.NextString();

            this.mockStorage
                .Setup(x => x.DeleteDeviceGroupAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(0));

            await this.controller.DeleteAsync(groupId);

            this.mockStorage
                .Verify(
                    x => x.DeleteDeviceGroupAsync(
                        It.Is<string>(s => s == groupId)),
                    Times.Once);
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