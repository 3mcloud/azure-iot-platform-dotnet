// <copyright file="DeploymentServiceModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.IoTHubManager.Services.Helpers;

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class DeploymentServiceModel : Audit
    {
        public DeploymentServiceModel(Configuration deployment)
        {
            if (string.IsNullOrEmpty(deployment.Id))
            {
                throw new ArgumentException($"Invalid deploymentId provided {deployment.Id}");
            }

            this.VerifyConfigurationLabel(deployment, ConfigurationsHelper.DeploymentNameLabel);
            this.VerifyConfigurationLabel(deployment, ConfigurationsHelper.DeploymentGroupIdLabel);
            this.VerifyConfigurationLabel(deployment, ConfigurationsHelper.RmCreatedLabel);

            this.Id = deployment.Id;
            this.Name = deployment.Labels[ConfigurationsHelper.DeploymentNameLabel];
            this.CreatedDateTimeUtc = deployment.CreatedTimeUtc;
            this.DeviceGroupId = deployment.Labels[ConfigurationsHelper.DeploymentGroupIdLabel];

            if (deployment.Labels.ContainsKey(ConfigurationsHelper.DeploymentGroupNameLabel))
            {
                this.DeviceGroupName = deployment.Labels[ConfigurationsHelper.DeploymentGroupNameLabel];
            }

            if (deployment.Labels.ContainsKey(ConfigurationsHelper.DeploymentPackageNameLabel))
            {
                this.PackageName = deployment.Labels[ConfigurationsHelper.DeploymentPackageNameLabel];
            }

            this.Priority = deployment.Priority;

            if (ConfigurationsHelper.IsEdgeDeployment(deployment))
            {
                this.PackageType = PackageType.EdgeManifest;
            }
            else
            {
                this.PackageType = PackageType.DeviceConfiguration;
            }

            if (deployment.Labels.ContainsKey(ConfigurationsHelper.ConfigTypeLabel))
            {
                this.ConfigType = deployment.Labels[ConfigurationsHelper.ConfigTypeLabel];
            }
            else
            {
                this.ConfigType = string.Empty;
            }

            this.DeploymentMetrics = new DeploymentMetricsServiceModel(deployment.SystemMetrics, deployment.Metrics);
        }

        public DeploymentServiceModel()
        {
        }

        public DateTime CreatedDateTimeUtc { get; set; }

        public string Id { get; set; }

        public DeploymentMetricsServiceModel DeploymentMetrics { get; set; }

        public string DeviceGroupId { get; set; }

        public string DeviceGroupName { get; set; }

        public string DeviceGroupQuery { get; set; }

        public string Name { get; set; }

        public string PackageContent { get; set; }

        public string PackageName { get; set; }

        public string PackageId { get; set; }

        public int Priority { get; set; }

        public PackageType PackageType { get; set; }

        public string ConfigType { get; set; }

        public IEnumerable<string> DeviceIds { get; set; }

        public IList<string> Tags { get; set; }

        public string ETag { get; set; }

        public string TargetCondition { get; set; }

        private void VerifyConfigurationLabel(Configuration deployment, string labelName)
        {
            if (!deployment.Labels.ContainsKey(labelName))
            {
                throw new ArgumentException($"Configuration is missing necessary label {labelName}");
            }
        }
    }
}