// <copyright file="StorageTest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.AsaManager;
using Mmm.Iot.Common.Services.External.StorageAdapter;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.Common.TestHelpers;
using Mmm.Iot.Config.Services.Helpers;
using Mmm.Iot.Config.Services.Models;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using DeviceGroup = Mmm.Iot.Config.Services.Models.DeviceGroup;
using DeviceGroupCondition = Mmm.Iot.Config.Services.Models.DeviceGroupCondition;

namespace Mmm.Iot.Config.Services.Test
{
    public class StorageTest
    {
        // ReSharper disable once SA1401
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "This needs to be public for MemberData.")]
        public static IEnumerable<object[]> PackageDataTags = new List<object[]>
        {
            new object[] { new List<string> { "devicegroups.1", "devicegroups.*" }, "OR", 3 },
            new object[] { new List<string> { "devicegroups.2", "devicegroups.*" }, "AND", 1 },
            new object[] { new List<string> { "isOdd.1" }, "OR", 1 },
            new object[] { new List<string> { "devicegroups.*" }, "OR", 3 },
            new object[] { new List<string> { "isOdd.0", "devicegroups.*" }, "AND", 2 },
            new object[] { new List<string> { "isOdd.0", "isOdd.1" }, "AND", 0 },
        };

        private const string TenantId = "tenantid-1234";
        private const string UserId = "userId";
        private const string PackagesCollectionId = "packages";

        private const string EdgePackageJson =
            @"{
                    ""id"": ""tempid"",
                    ""schemaVersion"": ""1.0"",
                    ""content"": {
                        ""modulesContent"": {
                        ""$edgeAgent"": {
                            ""properties.desired"": {
                            ""schemaVersion"": ""1.0"",
                            ""runtime"": {
                                ""type"": ""docker"",
                                ""settings"": {
                                ""loggingOptions"": """",
                                ""minDockerVersion"": ""v1.25""
                                }
                            },
                            ""systemModules"": {
                                ""edgeAgent"": {
                                ""type"": ""docker"",
                                ""settings"": {
                                    ""image"": ""mcr.microsoft.com/azureiotedge-agent:1.0"",
                                    ""createOptions"": ""{}""
                                }
                                },
                                ""edgeHub"": {
                                ""type"": ""docker"",
                                ""settings"": {
                                    ""image"": ""mcr.microsoft.com/azureiotedge-hub:1.0"",
                                    ""createOptions"": ""{}""
                                },
                                ""status"": ""running"",
                                ""restartPolicy"": ""always""
                                }
                            },
                            ""modules"": {}
                            }
                        },
                        ""$edgeHub"": {
                            ""properties.desired"": {
                            ""schemaVersion"": ""1.0"",
                            ""routes"": {
                                ""route"": ""FROM /messages/* INTO $upstream""
                            },
                            ""storeAndForwardConfiguration"": {
                                ""timeToLiveSecs"": 7200
                            }
                            }
                        }
                        }
                    },
                    ""targetCondition"": ""*"",
                    ""priority"": 30,
                    ""labels"": {
                        ""Name"": ""Test""
                    },
                    ""createdTimeUtc"": ""2018-08-20T18:05:55.482Z"",
                    ""lastUpdatedTimeUtc"": ""2018-08-20T18:05:55.482Z"",
                    ""etag"": null,
                    ""metrics"": {
                        ""results"": {},
                        ""queries"": {}
                    }
                 }";

        private const string AdmPackageJson =
            @"{
                    ""id"": ""9a9690df-f037-4c3a-8fc0-8eaba687609d"",
                    ""schemaVersion"": ""1.0"",
                    ""labels"": {
                        ""Type"": ""DeviceConfiguration"",
                        ""Name"": ""Deployment-12"",
                        ""DeviceGroupId"": ""MxChip"",
                        ""RMDeployment"": ""True""
                    },
                    ""content"": {
                        ""deviceContent"": {
                            ""properties.desired.firmware"": {
                                ""fwVersion"": ""1.0.1"",
                                ""fwPackageURI"": ""https://cs4c496459d5c79x44d1x97a.blob.core.windows.net/firmware/FirmwareOTA.ino.bin"",
                                ""fwPackageCheckValue"": ""45cd"",
                                ""fwSize"": 568648
                            }
                        }
                    },
                    ""targetCondition"": ""Tags.isVan1='Y'"",
                    ""createdTimeUtc"": ""2018-11-10T23:50:30.938Z"",
                    ""lastUpdatedTimeUtc"": ""2018-11-10T23:50:30.938Z"",
                    ""priority"": 20,
                    ""systemMetrics"": {
                        ""results"": {
                            ""targetedCount"": 2,
                            ""appliedCount"": 2
                        },
                        ""queries"": {
                            ""Targeted"": ""select deviceId from devices where Tags.isVan1='Y'"",
                            ""Applied"": ""select deviceId from devices where Items.[[9a9690df-f037-4c3a-8fc0-8eaba687609d]].status = 'Applied'""
                        }
                    },
                    ""metrics"": {
                        ""results"": {},
                        ""queries"": {}
                    },
                    ""etag"": ""MQ==""
                    }";

        private readonly string azureMapsKey;
        private readonly Mock<IStorageAdapterClient> mockClient;
        private readonly Mock<IAsaManagerClient> mockAsaManager;
        private readonly Storage storage;
        private readonly Random rand;
        private string packageId = "myId";

        private List<string> tags = new List<string>
        {
            "alpha",
            "namespace.alpha",
            "alphanum3ric",
            "namespace.alphanum3ric",
            "1234567890",
            "namespace.1234567890",
        };

        public StorageTest()
        {
            this.rand = new Random();

            this.azureMapsKey = this.rand.NextString();
            this.mockClient = new Mock<IStorageAdapterClient>();
            this.mockAsaManager = new Mock<IAsaManagerClient>();
            TelemetryClient mockTelemetryClient = this.InitializeMockTelemetryChannel();

            this.mockAsaManager
                .Setup(x => x.BeginDeviceGroupsConversionAsync())
                .ReturnsAsync(new BeginConversionApiModel());

            this.storage = new Storage(
                this.mockClient.Object,
                this.mockAsaManager.Object,
                new AppConfig
                {
                    ConfigService = new ConfigServiceConfig
                    {
                        AzureMapsKey = this.azureMapsKey,
                    },
                    Global = new GlobalConfig
                    {
                        InstrumentationKey = "instrumentationkey",
                    },
                },
                new Mock<IPackageEventLog>().Object,
                new Mock<ILogger<Storage>>().Object);
        }

        [Fact]
        public async Task GetThemeAsyncTest()
        {
            var name = this.rand.NextString();
            var description = this.rand.NextString();

            this.mockClient
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ValueApiModel
                {
                    Data = JsonConvert.SerializeObject(new Theme
                    {
                        Name = name,
                        Description = description,
                    }),
                });

            var result = await this.storage.GetThemeAsync();

            this.mockClient
                .Verify(
                    x => x.GetAsync(
                        It.Is<string>(s => s == Storage.SolutionCollectionId),
                        It.Is<string>(s => s == Storage.ThemeKey)),
                    Times.Once);

            Assert.Equal(result.Name.ToString(), name);
            Assert.Equal(result.Description.ToString(), description);
            Assert.Equal(result.AzureMapsKey.ToString(), this.azureMapsKey);
        }

        [Fact]
        public async Task GetThemeAsyncDefaultTest()
        {
            this.mockClient
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new ResourceNotFoundException());

            var result = await this.storage.GetThemeAsync();

            this.mockClient
                .Verify(
                    x => x.GetAsync(
                        It.Is<string>(s => s == Storage.SolutionCollectionId),
                        It.Is<string>(s => s == Storage.ThemeKey)),
                    Times.Once);

            Assert.Equal(result.Name.ToString(), Theme.Default.Name);
            Assert.Equal(result.Description.ToString(), Theme.Default.Description);
            Assert.Equal(result.AzureMapsKey.ToString(), this.azureMapsKey);
        }

        [Fact]
        public async Task SetThemeAsyncTest()
        {
            var name = this.rand.NextString();
            var description = this.rand.NextString();

            var theme = new Theme
            {
                Name = name,
                Description = description,
            };

            this.mockClient
                .Setup(x => x.UpdateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new ValueApiModel
                {
                    Data = JsonConvert.SerializeObject(theme),
                });

            var result = await this.storage.SetThemeAsync(theme) as dynamic;

            this.mockClient
                .Verify(
                    x => x.UpdateAsync(
                        It.Is<string>(s => s == Storage.SolutionCollectionId),
                        It.Is<string>(s => s == Storage.ThemeKey),
                        It.Is<string>(s => s == JsonConvert.SerializeObject(theme)),
                        It.Is<string>(s => s == "*")),
                    Times.Once);

            Assert.Equal(result.Name.ToString(), name);
            Assert.Equal(result.Description.ToString(), description);
            Assert.Equal(result.AzureMapsKey.ToString(), this.azureMapsKey);
        }

        [Fact]
        public async Task GetUserSettingAsyncTest()
        {
            var id = this.rand.NextString();
            var name = this.rand.NextString();
            var description = this.rand.NextString();

            this.mockClient
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ValueApiModel
                {
                    Data = JsonConvert.SerializeObject(new
                    {
                        Name = name,
                        Description = description,
                    }),
                });

            var result = await this.storage.GetUserSetting(id) as dynamic;

            this.mockClient
                .Verify(
                    x => x.GetAsync(
                        It.Is<string>(s => s == Storage.UserCollectionId),
                        It.Is<string>(s => s == id)),
                    Times.Once);

            Assert.Equal(result.Name.ToString(), name);
            Assert.Equal(result.Description.ToString(), description);
        }

        [Fact]
        public async Task SetUserSettingAsyncTest()
        {
            var id = this.rand.NextString();
            var name = this.rand.NextString();
            var description = this.rand.NextString();

            var setting = new
            {
                Name = name,
                Description = description,
            };

            this.mockClient
                .Setup(x => x.UpdateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new ValueApiModel
                {
                    Data = JsonConvert.SerializeObject(setting),
                });

            var result = await this.storage.SetUserSetting(id, setting) as dynamic;

            this.mockClient
                .Verify(
                    x => x.UpdateAsync(
                        It.Is<string>(s => s == Storage.UserCollectionId),
                        It.Is<string>(s => s == id),
                        It.Is<string>(s => s == JsonConvert.SerializeObject(setting)),
                        It.Is<string>(s => s == "*")),
                    Times.Once);

            Assert.Equal(result.Name.ToString(), name);
            Assert.Equal(result.Description.ToString(), description);
        }

        [Fact]
        public async Task GetLogoShouldReturnExpectedLogo()
        {
            var image = this.rand.NextString();
            var type = this.rand.NextString();

            this.mockClient
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ValueApiModel
                {
                    Data = JsonConvert.SerializeObject(new Logo
                    {
                        Image = image,
                        Type = type,
                        IsDefault = false,
                    }),
                });

            var result = await this.storage.GetLogoAsync() as dynamic;

            this.mockClient
                .Verify(
                    x => x.GetAsync(
                        It.Is<string>(s => s == Storage.SolutionCollectionId),
                        It.Is<string>(s => s == Storage.LogoKey)),
                    Times.Once);

            Assert.Equal(image, result.Image.ToString());
            Assert.Equal(type, result.Type.ToString());
            Assert.Null(result.Name);
            Assert.False(result.IsDefault);
        }

        [Fact]
        public async Task GetLogoShouldReturnExpectedLogoAndName()
        {
            var image = this.rand.NextString();
            var type = this.rand.NextString();
            var name = this.rand.NextString();

            this.mockClient
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ValueApiModel
                {
                    Data = JsonConvert.SerializeObject(new Logo
                    {
                        Image = image,
                        Type = type,
                        Name = name,
                        IsDefault = false,
                    }),
                });

            var result = await this.storage.GetLogoAsync() as dynamic;

            this.mockClient
                .Verify(
                    x => x.GetAsync(
                        It.Is<string>(s => s == Storage.SolutionCollectionId),
                        It.Is<string>(s => s == Storage.LogoKey)),
                    Times.Once);

            Assert.Equal(image, result.Image.ToString());
            Assert.Equal(type, result.Type.ToString());
            Assert.Equal(name, result.Name.ToString());
            Assert.False(result.IsDefault);
        }

        [Fact]
        public async Task GetLogoShouldReturnDefaultLogoOnException()
        {
            this.mockClient
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new ResourceNotFoundException());

            var result = await this.storage.GetLogoAsync() as dynamic;

            this.mockClient
                .Verify(
                    x => x.GetAsync(
                        It.Is<string>(s => s == Storage.SolutionCollectionId),
                        It.Is<string>(s => s == Storage.LogoKey)),
                    Times.Once);

            Assert.Equal(Logo.Default.Image, result.Image.ToString());
            Assert.Equal(Logo.Default.Type, result.Type.ToString());
            Assert.Equal(Logo.Default.Name, result.Name.ToString());
            Assert.True(result.IsDefault);
        }

        [Fact]
        public async Task SetLogoShouldNotOverwriteOldNameWithNull()
        {
            var image = this.rand.NextString();
            var type = this.rand.NextString();

            var oldImage = this.rand.NextString();
            var oldType = this.rand.NextString();
            var oldName = this.rand.NextString();

            var logo = new Logo
            {
                Image = image,
                Type = type,
            };

            Logo result = await this.SetLogoHelper(logo, oldImage, oldName, oldType, false);

            this.mockClient
                .Verify(
                    x => x.UpdateAsync(
                        It.Is<string>(s => s == Storage.SolutionCollectionId),
                        It.Is<string>(s => s == Storage.LogoKey),
                        It.Is<string>(s => s == JsonConvert.SerializeObject(logo)),
                        It.Is<string>(s => s == "*")),
                    Times.Once);

            Assert.Equal(image, result.Image.ToString());
            Assert.Equal(type, result.Type.ToString());

            // If name is not set, old name should remain
            Assert.Equal(oldName, result.Name.ToString());
            Assert.False(result.IsDefault);
        }

        [Fact]
        public async Task SetLogoShouldSetAllPartsOfLogoIfNotNull()
        {
            var image = this.rand.NextString();
            var type = this.rand.NextString();
            var name = this.rand.NextString();

            var oldImage = this.rand.NextString();
            var oldType = this.rand.NextString();
            var oldName = this.rand.NextString();

            var logo = new Logo
            {
                Image = image,
                Type = type,
                Name = name,
            };

            Logo result = await this.SetLogoHelper(logo, oldImage, oldName, oldType, false);

            Assert.Equal(image, result.Image.ToString());
            Assert.Equal(type, result.Type.ToString());
            Assert.Equal(name, result.Name.ToString());
            Assert.False(result.IsDefault);
        }

        [Fact]
        public async Task GetAllDeviceGroupsAsyncTest()
        {
            var groups = new[]
            {
                new DeviceGroup
                {
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
                },
                new DeviceGroup
                {
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
                },
                new DeviceGroup
                {
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
                },
            };

            var items = groups.Select(g => new ValueApiModel
            {
                Key = this.rand.NextString(),
                Data = JsonConvert.SerializeObject(g),
                ETag = this.rand.NextString(),
            }).ToList();

            this.mockClient
                .Setup(x => x.GetAllAsync(It.IsAny<string>()))
                .ReturnsAsync(new ValueListApiModel { Items = items });

            var result = (await this.storage.GetAllDeviceGroupsAsync()).ToList();

            this.mockClient
                .Verify(
                    x => x.GetAllAsync(
                        It.Is<string>(s => s == Storage.DeviceGroupCollectionId)),
                    Times.Once);

            Assert.Equal(result.Count, groups.Length);
            foreach (var g in result)
            {
                var item = items.Single(i => i.Key == g.Id);
                var group = JsonConvert.DeserializeObject<DeviceGroup>(item.Data);
                Assert.Equal(g.DisplayName, group.DisplayName);
                Assert.Equal(g.Conditions.First().Key, group.Conditions.First().Key);
                Assert.Equal(g.Conditions.First().Operator, group.Conditions.First().Operator);
                Assert.Equal(g.Conditions.First().Value, group.Conditions.First().Value);
            }
        }

        [Fact]
        public async Task GetDeviceGroupsAsyncTest()
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

            this.mockClient
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ValueApiModel
                {
                    Key = groupId,
                    Data = JsonConvert.SerializeObject(new DeviceGroup
                    {
                        DisplayName = displayName,
                        Conditions = conditions,
                    }),
                    ETag = etag,
                });

            var result = await this.storage.GetDeviceGroupAsync(groupId);

            this.mockClient
                .Verify(
                    x => x.GetAsync(
                        It.Is<string>(s => s == Storage.DeviceGroupCollectionId),
                        It.Is<string>(s => s == groupId)),
                    Times.Once);

            Assert.Equal(result.DisplayName, displayName);
            Assert.Equal(result.Conditions.First().Key, conditions.First().Key);
            Assert.Equal(result.Conditions.First().Operator, conditions.First().Operator);
            Assert.Equal(result.Conditions.First().Value, conditions.First().Value);
        }

        [Fact]
        public async Task CreateDeviceGroupAsyncTest()
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

            var group = new DeviceGroup
            {
                DisplayName = displayName,
                Conditions = conditions,
            };

            this.mockClient
                .Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ValueApiModel
                {
                    Key = groupId,
                    Data = JsonConvert.SerializeObject(group),
                    ETag = etag,
                });

            this.mockClient
                .Setup(x => x.GetAllAsync(
                    It.IsAny<string>()))
                .ReturnsAsync(new ValueListApiModel { Items = new List<ValueApiModel>() });

            var result = await this.storage.CreateDeviceGroupAsync(group);

            this.mockClient
                .Verify(
                    x => x.CreateAsync(
                        It.Is<string>(s => s == Storage.DeviceGroupCollectionId),
                        It.Is<string>(s => s == JsonConvert.SerializeObject(
                                               group,
                                               Formatting.Indented,
                                               new JsonSerializerSettings
                                               { NullValueHandling = NullValueHandling.Ignore }))),
                    Times.Once);

            this.mockAsaManager
                .Verify(
                    x => x.BeginDeviceGroupsConversionAsync(),
                    Times.Once);

            Assert.Equal(result.Id, groupId);
            Assert.Equal(result.DisplayName, displayName);
            Assert.Equal(result.Conditions.First().Key, conditions.First().Key);
            Assert.Equal(result.Conditions.First().Operator, conditions.First().Operator);
            Assert.Equal(result.Conditions.First().Value, conditions.First().Value);
            Assert.Equal(result.ETag, etag);
        }

        [Fact]
        public async Task CreateDeviceGroupAsyncThrowsConflicatingResourceOnExistingDisplayNameTest()
        {
            var existingGroup = new DeviceGroup
            {
                Id = this.rand.NextString(),
                DisplayName = this.rand.NextString(),
                Conditions = new List<DeviceGroupCondition>(),
            };

            this.mockClient
                .Setup(x => x.GetAllAsync(
                    It.IsAny<string>()))
                .ReturnsAsync(new ValueListApiModel
                {
                    Items = new List<ValueApiModel>
                    {
                        new ValueApiModel
                        {
                            Key = existingGroup.Id,
                            Data = JsonConvert.SerializeObject(existingGroup),
                            ETag = this.rand.NextString(),
                        },
                    },
                });

            var newGroup = new DeviceGroup
            {
                Id = this.rand.NextString(),
                DisplayName = existingGroup.DisplayName,
                Conditions = new List<DeviceGroupCondition>(),
            };

            await Assert.ThrowsAsync<ConflictingResourceException>(async () =>
                await this.storage.CreateDeviceGroupAsync(newGroup));
        }

        [Fact]
        public async Task UpdateDeviceGroupAsyncTest()
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

            var group = new DeviceGroup
            {
                DisplayName = displayName,
                Conditions = conditions,
            };

            this.mockClient
                .Setup(x => x.UpdateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new ValueApiModel
                {
                    Key = groupId,
                    Data = JsonConvert.SerializeObject(group),
                    ETag = etagNew,
                });

            this.mockClient
                .Setup(x => x.GetAllAsync(
                    It.IsAny<string>()))
                .ReturnsAsync(new ValueListApiModel { Items = new List<ValueApiModel>() });

            var result = await this.storage.UpdateDeviceGroupAsync(groupId, group, etagOld);

            this.mockClient
                .Verify(
                    x => x.UpdateAsync(
                        It.Is<string>(s => s == Storage.DeviceGroupCollectionId),
                        It.Is<string>(s => s == groupId),
                        It.Is<string>(s => s == JsonConvert.SerializeObject(
                                               group,
                                               Formatting.Indented,
                                               new JsonSerializerSettings
                                               { NullValueHandling = NullValueHandling.Ignore })),
                        It.Is<string>(s => s == etagOld)),
                    Times.Once);

            this.mockAsaManager
                .Verify(
                    x => x.BeginDeviceGroupsConversionAsync(),
                    Times.Once);

            Assert.Equal(result.Id, groupId);
            Assert.Equal(result.DisplayName, displayName);
            Assert.Equal(result.Conditions.First().Key, conditions.First().Key);
            Assert.Equal(result.Conditions.First().Operator, conditions.First().Operator);
            Assert.Equal(result.Conditions.First().Value, conditions.First().Value);
            Assert.Equal(result.ETag, etagNew);
        }

        [Fact]
        public async Task UpdateDeviceGroupAsyncThrowsConflicatingResourceOnExistingDisplayNameTest()
        {
            var existingGroup = new DeviceGroup
            {
                Id = this.rand.NextString(),
                DisplayName = this.rand.NextString(),
                Conditions = new List<DeviceGroupCondition>(),
            };

            this.mockClient
                .Setup(x => x.GetAllAsync(
                    It.IsAny<string>()))
                .ReturnsAsync(new ValueListApiModel
                {
                    Items = new List<ValueApiModel>
                    {
                        new ValueApiModel
                        {
                            Key = existingGroup.Id,
                            Data = JsonConvert.SerializeObject(existingGroup),
                            ETag = this.rand.NextString(),
                        },
                    },
                });

            var newGroup = new DeviceGroup
            {
                Id = this.rand.NextString(),
                DisplayName = existingGroup.DisplayName,
                Conditions = new List<DeviceGroupCondition>(),
            };

            await Assert.ThrowsAsync<ConflictingResourceException>(async () =>
                await this.storage.UpdateDeviceGroupAsync(newGroup.Id, newGroup, this.rand.NextString()));
        }

        [Fact]
        public async Task DeleteDeviceGroupAsyncTest()
        {
            var groupId = this.rand.NextString();

            this.mockClient
                .Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(0));

            await this.storage.DeleteDeviceGroupAsync(groupId);

            this.mockClient
                .Verify(
                    x => x.DeleteAsync(
                        It.Is<string>(s => s == Storage.DeviceGroupCollectionId),
                        It.Is<string>(s => s == groupId)),
                    Times.Once);

            this.mockAsaManager
                .Verify(
                    x => x.BeginDeviceGroupsConversionAsync(),
                    Times.Once);
        }

        [Fact]
        public async Task AddEdgePackageTest()
        {
            // Arrange
            const string collectionId = "packages";
            const string key = "package name";
            var pkg = new PackageServiceModel
            {
                Id = string.Empty,
                Name = key,
                PackageType = PackageType.EdgeManifest,
                ConfigType = string.Empty,
                Content = EdgePackageJson,
            };
            var value = JsonConvert.SerializeObject(pkg);

            this.mockClient
                .Setup(x => x.CreateAsync(
                    It.Is<string>(i => i == collectionId),
                    It.IsAny<string>()))
                .ReturnsAsync(new ValueApiModel
                {
                    Key = key,
                    Data = value,
                });

            // Act
            var result = await this.storage.AddPackageAsync(pkg, UserId, TenantId);

            // Assert
            Assert.Equal(pkg.Name, result.Name);
            Assert.Equal(pkg.PackageType, result.PackageType);
            Assert.Equal(pkg.Content, result.Content);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AddADMPackageTest(bool isCustomConfigType)
        {
            // Arrange
            const string collectionId = "packages";
            const string key = "package name";
            string configType = isCustomConfigType ? "Custom-config" : ConfigType.Firmware.ToString();

            var pkg = new PackageServiceModel
            {
                Id = string.Empty,
                Name = key,
                PackageType = PackageType.DeviceConfiguration,
                Content = AdmPackageJson,
                ConfigType = configType,
            };

            var value = JsonConvert.SerializeObject(pkg);

            this.mockClient
                .Setup(x => x.CreateAsync(
                    It.Is<string>(i => i == collectionId),
                    It.IsAny<string>()))
                .ReturnsAsync(new ValueApiModel
                {
                    Key = key,
                    Data = value,
                });

            const string configKey = "config-types";

            this.mockClient
                .Setup(x => x.UpdateAsync(
                    It.Is<string>(i => i == collectionId),
                    It.Is<string>(i => i == configKey),
                    It.Is<string>(i => i == ConfigType.Firmware.ToString()),
                    It.Is<string>(i => i == "*")))
                .ReturnsAsync(new ValueApiModel
                {
                    Key = key,
                    Data = value,
                });

            this.mockClient
                .Setup(x => x.GetAsync(
                    It.Is<string>(i => i == collectionId),
                    It.Is<string>(i => i == configKey)))
                .ThrowsAsync(new ResourceNotFoundException());

            // Act
            var result = await this.storage.AddPackageAsync(pkg, UserId, TenantId);

            // Assert
            Assert.Equal(pkg.Name, result.Name);
            Assert.Equal(pkg.PackageType, result.PackageType);
            Assert.Equal(pkg.Content, result.Content);
            Assert.Equal(pkg.ConfigType, result.ConfigType);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ListPackagesTest(bool isEdgeManifest)
        {
            // Arrange
            const string collectionId = "packages";
            const string id = "packageId";
            const string name = "packageName";
            const string content = "{}";

            int[] idx = new int[] { 0, 1, 2 };
            var packages = idx.Select(i => new PackageServiceModel()
            {
                Id = id + i,
                Name = name + i,
                Content = content + i,
                PackageType = (i == 0) ? PackageType.DeviceConfiguration : PackageType.EdgeManifest,
                ConfigType = (i == 0) ? ConfigType.Firmware.ToString() : string.Empty,
            }).ToList();

            this.mockClient
                .Setup(x => x.GetAllAsync(
                    It.Is<string>(i => (i == collectionId))))
                .ReturnsAsync(new ValueListApiModel
                {
                    Items = new List<ValueApiModel>()
                    {
                        new ValueApiModel()
                            { Key = string.Empty, Data = JsonConvert.SerializeObject(packages[0]) },
                        new ValueApiModel()
                            { Key = string.Empty, Data = JsonConvert.SerializeObject(packages[1]) },
                        new ValueApiModel()
                            { Key = string.Empty, Data = JsonConvert.SerializeObject(packages[2]) },
                    },
                });

            // Act
            var packageType = isEdgeManifest ? PackageType.EdgeManifest.ToString() : PackageType.DeviceConfiguration.ToString();

            var configType = isEdgeManifest ? string.Empty : ConfigType.Firmware.ToString();

            try
            {
                var resultPackages = await this.storage.GetFilteredPackagesAsync(
                    packageType,
                    configType,
                    null,
                    null);

                // Assert
                var pkg = resultPackages.First();
                Assert.Equal(packageType, pkg.PackageType.ToString());
                Assert.Equal(configType, pkg.ConfigType);
            }
            catch (Exception e)
            {
                Assert.False(isEdgeManifest, e.Message);
            }
        }

        [Theory]
        [MemberData(nameof(PackageDataTags))]
        public async Task ListPackagesByTags(List<string> tags, string tagOperator, int numResult)
        {
            // Arrange
            const string collectionId = "packages";
            const string id = "packageId";
            const string name = "packageName";
            const string content = "{}";

            int[] idx = new int[] { 0, 1, 2 };
            var packages = idx.Select(i => new PackageServiceModel()
            {
                Id = id + i,
                Name = name + i,
                Content = content + i,
                PackageType = (i == 0) ? PackageType.DeviceConfiguration : PackageType.EdgeManifest,
                ConfigType = (i == 0) ? ConfigType.Firmware.ToString() : string.Empty,
                Tags = new List<string>()
                {
                    "devicegroups." + i,
                    "devicegroups.*",
                    "isOdd." + (i % 2),
                },
            }).ToList();

            this.mockClient
                .Setup(x => x.GetAllAsync(
                    It.Is<string>(i => (i == collectionId))))
                .ReturnsAsync(new ValueListApiModel
                {
                    Items = new List<ValueApiModel>()
                    {
                        new ValueApiModel()
                            { Key = string.Empty, Data = JsonConvert.SerializeObject(packages[0]) },
                        new ValueApiModel()
                            { Key = string.Empty, Data = JsonConvert.SerializeObject(packages[1]) },
                        new ValueApiModel()
                            { Key = string.Empty, Data = JsonConvert.SerializeObject(packages[2]) },
                    },
                });

            // Act
            try
            {
                var resultPackages = await this.storage.GetAllPackagesAsync(
                    tags,
                    tagOperator);

                // Assert
                Assert.True(resultPackages.Count() == numResult);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [Fact]
        public async Task ListConfigurationsTest()
        {
            const string collectionId = "packages";
            const string configKey = "config-types";

            // Arrange
            this.mockClient
                .Setup(x => x.GetAsync(
                    It.Is<string>(i => i == collectionId),
                    It.Is<string>(i => i == configKey)))
                .ThrowsAsync(new ResourceNotFoundException());

            // Act
            var result = await this.storage.GetConfigTypesListAsync();

            // Assert
            Assert.Empty(result.ConfigTypes);
        }

        [Fact]
        public async Task InvalidPackageThrowsTest()
        {
            // Arrange
            var pkg = new PackageServiceModel
            {
                Id = string.Empty,
                Name = "testpackage",
                PackageType = PackageType.EdgeManifest,
                Content = "InvalidPackage",
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidInputException>(async () =>
                await this.storage.AddPackageAsync(pkg, It.IsAny<string>(), It.IsAny<string>()));
        }

        [Fact]
        public async Task DeletePackageAsyncTest()
        {
            // Arrange
            var packageId = this.rand.NextString();

            this.mockClient
                .Setup(x => x.DeleteAsync(
                    It.Is<string>(s => s == PackagesCollectionId),
                    It.Is<string>(s => s == packageId)))
                .Returns(Task.FromResult(0));

            // Act
            await this.storage.DeletePackageAsync(packageId, UserId, TenantId);

            // Assert
            this.mockClient
                .Verify(
                    x => x.DeleteAsync(
                        It.Is<string>(s => s == PackagesCollectionId),
                        It.Is<string>(s => s == packageId)),
                    Times.Once);
        }

        [Theory]
        [Trait(Constants.Type, Constants.UnitTest)]
        [InlineData("alpha", 1)]
        [InlineData("namespace.alpha", 1)]
        [InlineData("alphanum3ric", 1)]
        [InlineData("namespace.alphanum3ric", 1)]
        [InlineData("1234567890", 1)]
        [InlineData("namespace.1234567890", 1)]
        [InlineData(null, 0)]
#pragma warning disable SA1122
        [InlineData("", 0)]
#pragma warning restore SA1122
        public async Task PackageTagsCanBeAdded(string tag, int expectedTagCount)
        {
            // Arrange
            this.mockClient.Setup(m => m.GetAsync(Storage.PackagesCollectionId, this.packageId))
                .ReturnsAsync(new ValueApiModel { Data = @"{ ""Tags"": []}" });
            this.mockClient
                .Setup(m => m.UpdateAsync(
                    Storage.PackagesCollectionId,
                    this.packageId,
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new ValueApiModel { Data = $@"{{ ""Tags"": [""{tag}""]}}" });

            // Act
            var package = await this.storage.AddPackageTagAsync(this.packageId, tag, UserId);

            // Assert
            Assert.Equal(expectedTagCount, package.Tags.Count);
            if (expectedTagCount > 0)
            {
                Assert.Contains(tag, package.Tags);
            }
        }

        [Theory]
        [Trait(Constants.Type, Constants.UnitTest)]
        [InlineData("alpha", 5)]
        [InlineData("namespace.alpha", 5)]
        [InlineData("alphanum3ric", 5)]
        [InlineData("namespace.alphanum3ric", 5)]
        [InlineData("1234567890", 5)]
        [InlineData("namespace.1234567890", 5)]
        [InlineData("non-existent-tag", 6)]
        [InlineData(null, 6)]
#pragma warning disable SA1122
        [InlineData("", 6)]
#pragma warning restore SA1122
        public async Task PackageTagsCanBeRemoved(string tag, int expectedTagCount)
        {
            // Arrange
            var value = new ValueApiModel
            {
                Key = this.packageId,
                Data = JsonConvert.SerializeObject(new PackageServiceModel { Tags = this.tags.ToList() }),
            };
            this.mockClient.Setup(m => m.GetAsync(Storage.PackagesCollectionId, this.packageId)).ReturnsAsync(value);
            this.mockClient.Setup(m =>
                    m.UpdateAsync(Storage.PackagesCollectionId, this.packageId, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string c, string id, string v, string e) =>
                {
                    return new ValueApiModel { Key = id, Data = v };
                });

            // Act
            var result = await this.storage.RemovePackageTagAsync(this.packageId, tag, UserId);

            // Assert
            Assert.Equal(expectedTagCount, result.Tags.Count);
        }

        [Fact]
        public async Task AddingPackageTagsIsCaseInsensitive()
        {
            // Arrange
            var tag = "myTaG";
            this.mockClient.Setup(m => m.GetAsync(Storage.PackagesCollectionId, this.packageId))
                .ReturnsAsync(new ValueApiModel { Data = $@"{{ ""Tags"": [""{tag}""]}}" });

            // Act
            var package = await this.storage.AddPackageTagAsync(this.packageId, tag, UserId);

            // Assert
            Assert.Equal(1, package.Tags.Count);
            Assert.Contains(tag, package.Tags);
        }

        [Fact]
        public async Task RemovingPackageTagsIsCaseInsensitive()
        {
            // Arrange
            var tag = "myTaG";
            this.mockClient.Setup(m => m.GetAsync(
                    Storage.PackagesCollectionId,
                    this.packageId))
                .ReturnsAsync(new ValueApiModel { Data = $@"{{ ""Tags"": [""{tag}""]}}" });
            this.mockClient
                .Setup(m => m.UpdateAsync(
                    Storage.PackagesCollectionId,
                    this.packageId,
                    It.IsAny<string>(),
                    It.IsAny<string>())).ReturnsAsync(new ValueApiModel { Data = @"{ ""Tags"": []}" });

            // Act
            var package = await this.storage.RemovePackageTagAsync(this.packageId, "MYtAg", UserId);

            // Assert
            this.mockClient.Verify(
                m => m.UpdateAsync(
                    Storage.PackagesCollectionId,
                    this.packageId,
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Once);
        }

        private bool IsMatchingPackage(string pkgJson, string originalPkgJson)
        {
            const string dateCreatedField = "DateCreated";
            var createdPkg = JObject.Parse(pkgJson);
            var originalPkg = JObject.Parse(originalPkgJson);

            // To prevent false failures on unit tests we allow a couple of seconds diffence
            // when verifying the date created.
            var dateCreated = DateTimeOffset.Parse(createdPkg[dateCreatedField].ToString());
            var secondsDiff = (DateTimeOffset.UtcNow - dateCreated).TotalSeconds;
            if (secondsDiff > 3)
            {
                return false;
            }

            createdPkg.Remove(dateCreatedField);
            originalPkg.Remove(dateCreatedField);

            return JToken.DeepEquals(createdPkg, originalPkg);
        }

        private TelemetryClient InitializeMockTelemetryChannel()
        {
            // Application Insights TelemetryClient doesn't have an interface (and is sealed) hence implementation of a class that implements a mock
            MockTelemetryChannel mockTelemetryChannel = new MockTelemetryChannel();
            TelemetryConfiguration mockTelemetryConfiguration = new TelemetryConfiguration
            {
                TelemetryChannel = mockTelemetryChannel,
                InstrumentationKey = Guid.NewGuid().ToString(),
            };

            TelemetryClient mockTelemetryClient = new TelemetryClient(mockTelemetryConfiguration);
            return mockTelemetryClient;
        }

        private async Task<Logo> SetLogoHelper(Logo logo, string oldImage, string oldName, string oldType, bool isDefault)
        {
            this.mockClient
                .Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string id, string key, string value, string etag) => new ValueApiModel
                {
                    Data = value,
                });

            this.mockClient.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ValueApiModel
                {
                    Data = JsonConvert.SerializeObject(new Logo
                    {
                        Image = oldImage,
                        Type = oldType,
                        Name = oldName,
                        IsDefault = false,
                    }),
                });

            Logo result = await this.storage.SetLogoAsync(logo);

            this.mockClient
                .Verify(
                    x => x.UpdateAsync(
                        It.Is<string>(s => s == Storage.SolutionCollectionId),
                        It.Is<string>(s => s == Storage.LogoKey),
                        It.Is<string>(s => s == JsonConvert.SerializeObject(logo)),
                        It.Is<string>(s => s == "*")),
                    Times.Once);

            return result;
        }
    }
}