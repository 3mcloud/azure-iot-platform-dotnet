// <copyright file="DeploymentsTest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.External.CosmosDb;
using Mmm.Iot.Common.Services.External.StorageAdapter;
using Mmm.Iot.Common.Services.Helpers;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.Common.TestHelpers;
using Mmm.Iot.IoTHubManager.Services.External;
using Mmm.Iot.IoTHubManager.Services.Helpers;
using Mmm.Iot.IoTHubManager.Services.Models;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Mmm.Iot.IoTHubManager.Services.Test
{
    public class DeploymentsTest
    {
        private const string TestEdgePackageJson =
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

        private const string TestAdmPackageJson =
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
                    }mockIoTHub
                 }";

        private const string DeploymentNameLabel = "Name";
        private const string DeploymentGroupIdLabel = "DeviceGroupId";
        private const string DeploymentGroupNameLabel = "DeviceGroupName";
        private const string ConfigurationTypeLabel = "ConfigType";
        private const string RmCreatedLabel = "RMDeployment";
        private const string ResourceNotFoundException = "Mmm.Iot.Common.Services.Exceptions.ResourceNotSupportedException, Mmm.Iot.Common.Services, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
        private const string DeploymentPackageNameLabel = "PackageName";

        private readonly Deployments deployments;
        private readonly Mock<RegistryManager> registry;
        private readonly string ioTHubHostName = "mockIoTHub";
        private Mock<ITenantConnectionHelper> tenantHelper;
        private Mock<IConfigClient> packageConfigClient;
        private Mock<IStorageAdapterClient> storageAdapterClient;
        private Mock<IDevices> devices;
        private Mock<IStorageClient> storageClient;
        private string packageTypeLabel = "Type";

        public DeploymentsTest()
        {
            this.registry = new Mock<RegistryManager>();
            this.tenantHelper = new Mock<ITenantConnectionHelper>();
            this.packageConfigClient = new Mock<IConfigClient>();
            this.storageAdapterClient = new Mock<IStorageAdapterClient>();
            this.tenantHelper.Setup(e => e.GetIotHubName()).Returns(this.ioTHubHostName);
            this.tenantHelper.Setup(e => e.GetRegistry()).Returns(this.registry.Object);
            TelemetryClient mockTelemetryClient = this.InitializeMockTelemetryChannel();
            this.devices = new Mock<IDevices>();
            this.storageClient = new Mock<IStorageClient>();

            MockIdentity.MockClaims("one");
            this.deployments = new Deployments(
                new AppConfig
                {
                    Global = new GlobalConfig
                    {
                        InstrumentationKey = "instrumentationkey",
                    },
                },
                new Mock<ILogger<Deployments>>().Object,
                new Mock<IDeploymentEventLog>().Object,
                this.tenantHelper.Object,
                this.packageConfigClient.Object,
                this.storageAdapterClient.Object,
                this.devices.Object,
                this.storageClient.Object,
                null);
        }

        [Theory]
        [Trait(Constants.Type, Constants.UnitTest)]
        [InlineData("depname", "dvcgroupid", "dvcquery", TestEdgePackageJson, 10, "")]
        [InlineData("", "dvcgroupid", "dvcquery", TestEdgePackageJson, 10, "System.ArgumentNullException")]
        [InlineData("depname", "", "dvcquery", TestEdgePackageJson, 10, "System.ArgumentNullException")]
        [InlineData("depname", "dvcgroupid", "", TestEdgePackageJson, 10, "System.ArgumentNullException")]
        [InlineData("depname", "dvcgroupid", "dvcquery", "", 10, "System.ArgumentNullException")]
        [InlineData("depname", "dvcgroupid", "dvcquery", TestEdgePackageJson, -1, "System.ArgumentOutOfRangeException")]
        public async Task CreateDeploymentTest(
            string deploymentName,
            string deviceGroupId,
            string deviceGroupQuery,
            string packageContent,
            int priority,
            string expectedException)
        {
            // Arrange
            var depModel = new DeploymentServiceModel()
            {
                Name = deploymentName,
                DeviceGroupId = deviceGroupId,
                DeviceGroupQuery = deviceGroupQuery,
                PackageContent = packageContent,
                PackageType = PackageType.EdgeManifest,
                Priority = priority,
            };

            var userId = "testUser";
            var tenantId = "testTenat";

            var existingConfig = new Configuration("test-config1")
            {
                Labels = new Dictionary<string, string>()
                {
                    { DeploymentNameLabel, deploymentName },
                    { this.packageTypeLabel, PackageType.EdgeManifest.ToString() },
                    { DeploymentGroupIdLabel, deviceGroupId },
                    { RmCreatedLabel, bool.TrueString },
                },
                Priority = priority,
            };

            var newConfig = new Configuration("test-config")
            {
                Labels = new Dictionary<string, string>()
                {
                    { DeploymentNameLabel, deploymentName },
                    { this.packageTypeLabel, PackageType.EdgeManifest.ToString() },
                    { DeploymentGroupIdLabel, deviceGroupId },
                    { RmCreatedLabel, bool.TrueString },
                },
                Priority = priority,
            };

            this.registry.Setup(r => r.AddConfigurationAsync(It.Is<Configuration>(c =>
                    c.Labels.ContainsKey(DeploymentNameLabel) &&
                    c.Labels.ContainsKey(DeploymentGroupIdLabel) &&
                    c.Labels.ContainsKey(RmCreatedLabel) &&
                    c.Labels[DeploymentNameLabel] == deploymentName &&
                    c.Labels[DeploymentGroupIdLabel] == deviceGroupId &&
                    c.Labels[RmCreatedLabel] == bool.TrueString)))
                .ReturnsAsync(newConfig);

            this.registry.Setup(r => r.CreateQuery(It.IsAny<string>())).Returns(new ResultQuery(0));

            this.registry.Setup(r => r.GetConfigurationAsync(It.IsAny<string>())).ReturnsAsync(existingConfig);

            this.tenantHelper.Setup(e => e.GetRegistry()).Returns(this.registry.Object);

            this.storageAdapterClient.Setup(s => s.UpdateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>())).ReturnsAsync(new ValueApiModel());

            this.storageAdapterClient.Setup(s => s.CreateAsync(
            It.IsAny<string>(),
            It.IsAny<string>())).ReturnsAsync(new ValueApiModel());

            var deploymentStorageData = this.CreateDeploymentStorageData(0);
            this.storageAdapterClient.Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(deploymentStorageData);

            this.devices.Setup(d => d.GetListAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new DeviceServiceListModel(new List<DeviceServiceModel>()));

            var configurations = new List<Configuration>();
            configurations.Add(existingConfig);

            this.registry.Setup(r => r.GetConfigurationsAsync(100)).ReturnsAsync(configurations);
            this.tenantHelper.Setup(e => e.GetRegistry()).Returns(this.registry.Object);

            // Act
            if (string.IsNullOrEmpty(expectedException))
            {
                var createdDeployment = await this.deployments.CreateAsync(depModel, userId, tenantId);

                // Assert
                Assert.False(string.IsNullOrEmpty(createdDeployment.Id));
                Assert.Equal(deploymentName, createdDeployment.Name);
                Assert.Equal(deviceGroupId, createdDeployment.DeviceGroupId);
                Assert.Equal(priority, createdDeployment.Priority);
            }
            else
            {
                await Assert.ThrowsAsync(
                    Type.GetType(expectedException),
                    async () => await this.deployments.CreateAsync(depModel, userId, tenantId));
            }
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task InvalidRmConfigurationTest()
        {
            // Arrange
            var configuration = this.CreateConfiguration(0, false);

            this.registry.Setup(r => r.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(configuration);
            this.tenantHelper.Setup(e => e.GetRegistry()).Returns(this.registry.Object);

            // Act & Assert
            await Assert.ThrowsAsync(
                Type.GetType(ResourceNotFoundException),
                async () => await this.deployments.GetAsync(configuration.Id));
        }

        [Theory]
        [Trait(Constants.Type, Constants.UnitTest)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        public async Task GetDeploymentsTest(int numDeployments)
        {
            // Arrange
            var configurations = new List<Configuration>();
            for (int i = numDeployments - 1; i >= 0; i--)
            {
                configurations.Add(this.CreateConfiguration(i, true));
            }

            this.registry.Setup(r => r.GetConfigurationsAsync(100)).ReturnsAsync(configurations);
            this.tenantHelper.Setup(e => e.GetRegistry()).Returns(this.registry.Object);

            // Act
            var returnedDeployments = await this.deployments.ListAsync();

            // Assert
            Assert.Equal(numDeployments, returnedDeployments.Items.Count);

            for (int i = 0; i < numDeployments; i++)
            {
                Assert.True(returnedDeployments.Items.Exists(item => item.Name == $"deployment{i}"));
            }
        }

        [Theory]
        [Trait(Constants.Type, Constants.UnitTest)]
        [InlineData(1)]
        public async Task GetDeploymentsFromStorageTest(int numDeployments)
        {
            // Arrange
            var configurations = new List<Configuration>();
            this.registry.Setup(r => r.GetConfigurationsAsync(100)).ReturnsAsync(configurations);
            this.tenantHelper.Setup(e => e.GetRegistry()).Returns(this.registry.Object);

            var storageData = new List<ValueApiModel>();
            for (int i = numDeployments - 1; i >= 0; i--)
            {
                storageData.Add(this.CreateDeploymentStorageData(i));
            }

            this.storageAdapterClient.Setup(r => r.GetAllAsync(It.IsAny<string>())).ReturnsAsync(new ValueListApiModel() { Items = storageData });

            // Act
            var returnedDeployments = await this.deployments.ListFromStorageAsync();

            // Assert
            Assert.Equal(numDeployments, returnedDeployments.Items.Count);

            for (int i = 0; i < numDeployments; i++)
            {
                Assert.True(returnedDeployments.Items.Exists(item => item.Name == $"deployment{i}"));
            }
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task GetDeploymentsWithDeviceStatusTest()
        {
            // Arrange
            var configuration = this.CreateConfiguration(0, true);
            var deploymentId = configuration.Id;
            this.registry.Setup(r => r.GetConfigurationAsync(deploymentId)).ReturnsAsync(configuration);

            IQuery queryResult = new ResultQuery(3);
            this.registry.Setup(r => r.CreateQuery(It.IsAny<string>())).Returns(queryResult);

            this.tenantHelper.Setup(e => e.GetRegistry()).Returns(this.registry.Object);

            var deploymentStorageData = this.CreateDeploymentStorageData(0);
            this.storageAdapterClient.Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(deploymentStorageData);

            // Act
            var returnedDeployment = await this.deployments.GetAsync(deploymentId, true);
            var deviceStatuses = returnedDeployment.DeploymentMetrics.DeviceStatuses;
            Assert.Equal(3, deviceStatuses.Count);

            // Assert
            returnedDeployment = await this.deployments.GetAsync(deploymentId);
            deviceStatuses = returnedDeployment.DeploymentMetrics.DeviceStatuses;
            Assert.Null(deviceStatuses);

            returnedDeployment = await this.deployments.GetAsync("deployment0", false, false);
            deviceStatuses = returnedDeployment.DeploymentMetrics.DeviceStatuses;
            Assert.Null(deviceStatuses);

            returnedDeployment = await this.deployments.GetAsync("deployment0", true, false);
            deviceStatuses = returnedDeployment.DeploymentMetrics.DeviceStatuses;
            Assert.NotNull(deviceStatuses);
        }

        [Theory]
        [Trait(Constants.Type, Constants.UnitTest)]
        [InlineData(true, true, true)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        [InlineData(false, true, true)]
        [InlineData(true, false)]
        [InlineData(true, false, true)]
        public async Task GetDeploymentTypeTest(bool isEdgeContent, bool addLabel, bool isEdgeLabel = false)
        {
            // Arrange
            var content = new ConfigurationContent()
            {
                ModulesContent = isEdgeContent ? new Dictionary<string, IDictionary<string, object>>() : null,
                DeviceContent = !isEdgeContent ? new Dictionary<string, object>() : null,
            };

            var label = string.Empty;

            if (addLabel)
            {
                label = isEdgeLabel ? PackageType.EdgeManifest.ToString() :
                    PackageType.DeviceConfiguration.ToString();
            }

            var configuration = new Configuration("test-config")
            {
                Labels = new Dictionary<string, string>()
                {
                    { DeploymentNameLabel, string.Empty },
                    { DeploymentGroupIdLabel, string.Empty },
                    { this.packageTypeLabel, label },
                    { ConfigurationTypeLabel, "CustomConfig" },
                    { RmCreatedLabel, bool.TrueString },
                },
                Content = content,
            };

            var deploymentId = configuration.Id;
            this.registry.Setup(r => r.GetConfigurationAsync(deploymentId)).ReturnsAsync(configuration);
            this.registry.Setup(r => r.CreateQuery(It.IsAny<string>())).Returns(new ResultQuery(0));

            this.tenantHelper.Setup(e => e.GetRegistry()).Returns(this.registry.Object);

            // Act
            var returnedDeployment = await this.deployments.GetAsync(deploymentId);

            // Assert Should returned Deplyment PackageType according to label
            if (addLabel)
            {
                if (isEdgeLabel)
                {
                    Assert.Equal(PackageType.EdgeManifest, returnedDeployment.PackageType);
                }
                else
                {
                    Assert.Equal(PackageType.DeviceConfiguration, returnedDeployment.PackageType);
                }
            }
            else
            {
                if (isEdgeContent)
                {
                    Assert.Equal(PackageType.EdgeManifest, returnedDeployment.PackageType);
                }
                else
                {
                    Assert.Equal(PackageType.DeviceConfiguration, returnedDeployment.PackageType);
                }
            }
        }

        [Theory]
        [Trait(Constants.Type, Constants.UnitTest)]
        [InlineData(false)]
        public async Task GetDeploymentMetricsTest(bool isEdgeDeployment)
        {
            // Arrange
            var content = new ConfigurationContent()
            {
                ModulesContent = isEdgeDeployment ? new Dictionary<string, IDictionary<string, object>>() : null,
                DeviceContent = !isEdgeDeployment ? new Dictionary<string, object>() : null,
            };

            var label = isEdgeDeployment ? PackageType.EdgeManifest.ToString() : PackageType.DeviceConfiguration.ToString();

            var firmware = "Firmware";

            var configuration = new Configuration("test-config")
            {
                Labels = new Dictionary<string, string>()
                {
                    { DeploymentNameLabel, string.Empty },
                    { DeploymentGroupIdLabel, string.Empty },
                    { this.packageTypeLabel, label },
                    { ConfigurationTypeLabel, firmware },
                    { RmCreatedLabel, bool.TrueString },
                },
                Content = content,
            };

            var deploymentId = configuration.Id;
            this.registry.Setup(r => r.GetConfigurationAsync(deploymentId)).ReturnsAsync(configuration);
            this.registry.Setup(r => r.CreateQuery(It.IsAny<string>())).Returns(new ResultQuery(0));
            this.tenantHelper.Setup(e => e.GetRegistry()).Returns(this.registry.Object);

            var deploymentStorageData = this.CreateDeploymentStorageData(0);
            this.storageAdapterClient.Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(deploymentStorageData);

            // Act
            var returnedDeployment = await this.deployments.GetAsync(deploymentId);

            // Assert Should return Deplyment metrics according to label
            Assert.NotNull(returnedDeployment.DeploymentMetrics.DeviceMetrics);
            Assert.Equal(3, returnedDeployment.DeploymentMetrics.DeviceMetrics.Count());

            returnedDeployment = await this.deployments.GetAsync("deployment0", false, false);
            Assert.NotNull(returnedDeployment.DeploymentMetrics.DeviceMetrics);
            Assert.Equal(3, returnedDeployment.DeploymentMetrics.DeviceMetrics.Count());
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task VerifyGroupAndPackageNameLabelsTest()
        {
            // Arrange
            var deviceGroupId = "dvcGroupId";
            var deviceGroupName = "dvcGroupName";
            var deviceGroupQuery = "dvcGroupQuery";
            var packageName = "packageName";
            var deploymentName = "depName";
            var priority = 10;
            var userid = "testUser";
            var tenantId = "testTenant";

            var depModel = new DeploymentServiceModel()
            {
                Name = deploymentName,
                DeviceGroupId = deviceGroupId,
                DeviceGroupName = deviceGroupName,
                DeviceGroupQuery = deviceGroupQuery,
                PackageContent = TestEdgePackageJson,
                PackageName = packageName,
                Priority = priority,
            };

            var newConfig = new Configuration("test-config")
            {
                Labels = new Dictionary<string, string>()
                {
                    { this.packageTypeLabel, PackageType.EdgeManifest.ToString() },
                    { DeploymentNameLabel, deploymentName },
                    { DeploymentGroupIdLabel, deviceGroupId },
                    { RmCreatedLabel, bool.TrueString },
                    { DeploymentGroupNameLabel, deviceGroupName },
                    { DeploymentPackageNameLabel, packageName },
                },
                Priority = priority,
            };

            this.registry.Setup(r => r.AddConfigurationAsync(It.Is<Configuration>(c =>
                    c.Labels.ContainsKey(DeploymentNameLabel) &&
                    c.Labels.ContainsKey(DeploymentGroupIdLabel) &&
                    c.Labels.ContainsKey(RmCreatedLabel) &&
                    c.Labels[DeploymentNameLabel] == deploymentName &&
                    c.Labels[DeploymentGroupIdLabel] == deviceGroupId &&
                    c.Labels[RmCreatedLabel] == bool.TrueString)))
                .ReturnsAsync(newConfig);

            this.tenantHelper.Setup(e => e.GetRegistry()).Returns(this.registry.Object);

            // Act
            var createdDeployment = await this.deployments.CreateAsync(depModel, userid, tenantId);

            // Assert
            Assert.False(string.IsNullOrEmpty(createdDeployment.Id));
            Assert.Equal(deploymentName, createdDeployment.Name);
            Assert.Equal(deviceGroupId, createdDeployment.DeviceGroupId);
            Assert.Equal(priority, createdDeployment.Priority);
            Assert.Equal(deviceGroupName, createdDeployment.DeviceGroupName);
            Assert.Equal(packageName, createdDeployment.PackageName);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task FilterOutNonRmDeploymentsTest()
        {
            // Arrange
            var configurations = new List<Configuration>
            {
                this.CreateConfiguration(0, true),
                this.CreateConfiguration(1, false),
            };

            this.registry.Setup(r => r.GetConfigurationsAsync(100))
                .ReturnsAsync(configurations);
            this.tenantHelper.Setup(e => e.GetRegistry()).Returns(this.registry.Object);

            // Act
            var returnedDeployments = await this.deployments.ListAsync();

            // Assert
            Assert.Single(returnedDeployments.Items);
            Assert.Equal("deployment0", returnedDeployments.Items[0].Name);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task GetPackagesTest()
        {
            // Arrange
            var packageId = Guid.NewGuid().ToString();
            this.packageConfigClient.Setup(m => m.GetPackageAsync(It.IsAny<string>())).ReturnsAsync(new PackageApiModel()
            {
                Id = packageId,
                Name = "Test-Package",
                ConfigType = "Firmware",
                Content = "Test-Content",
                PackageType = PackageType.DeviceConfiguration,
            });

            // Act
            var result = await this.deployments.GetPackageAsync(packageId);

            // Assert
            Assert.Equal(result.Id, packageId);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task GetDeviceGroupTest()
        {
            // Arrange
            var deviceId = Guid.NewGuid().ToString();
            var deviceGroupName = "Test-Group";
            this.packageConfigClient.Setup(m => m.GetDeviceGroupAsync(It.IsAny<string>())).ReturnsAsync(new DeviceGroup()
            {
                Id = deviceId,
                DisplayName = deviceGroupName,
                Conditions = new List<DeviceGroupCondition>(),
            });

            // Act
            var result = await this.deployments.GetDeviceGroupAsync(deviceId);

            // Assert
            Assert.Equal(result.Id, deviceId);
            Assert.Equal(result.DisplayName, deviceGroupName);
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

        private Configuration CreateConfiguration(int idx, bool addCreatedByRmLabel)
        {
            var conf = new Configuration("test-config" + idx)
            {
                Labels = new Dictionary<string, string>()
                {
                    { this.packageTypeLabel, PackageType.EdgeManifest.ToString() },
                    { DeploymentNameLabel, "deployment" + idx },
                    { DeploymentGroupIdLabel, "dvcGroupId" + idx },
                },
                Priority = 10,
            };

            if (addCreatedByRmLabel)
            {
                conf.Labels.Add(RmCreatedLabel, "true");
            }

            return conf;
        }

        private ValueApiModel CreateDeploymentStorageData(int idx)
        {
            // Arrange
            var deviceGroupId = "dvcGroupId";
            var deviceGroupName = "dvcGroupName";
            var deviceGroupQuery = "dvcGroupQuery";
            var packageName = "packageName";
            var deploymentName = "deployment";
            var priority = 10;
            var userid = "testUser";

            var deployment = new DeploymentServiceModel()
            {
                Name = deploymentName + idx,
                DeviceGroupId = deviceGroupId,
                DeviceGroupName = deviceGroupName,
                DeviceGroupQuery = deviceGroupQuery,
                PackageContent = TestEdgePackageJson,
                PackageName = packageName,
                Priority = priority,
                CreatedBy = userid,
                CreatedDateTimeUtc = DateTime.UtcNow,
                CreatedDateTime = DateTime.UtcNow,
                DeploymentMetrics = new DeploymentMetricsServiceModel() { DeviceStatuses = new Dictionary<string, DeploymentStatus> { { "device1", DeploymentStatus.Pending } } },
            };
            var jsonValue = JsonConvert.SerializeObject(deployment);
            ValueApiModel value = new ValueApiModel() { Key = idx.ToString(), Data = jsonValue };

            return value;
        }
    }
}