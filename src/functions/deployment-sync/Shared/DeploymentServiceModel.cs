// <copyright file="DeploymentServiceModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices;
using Mmm.Iot.Functions.DeploymentSync.Shared.Exceptions;
using Newtonsoft.Json;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class DeploymentServiceModel : Audit
    {
        public const string PackageTypeLabel = "Type";
        public const string ConfigTypeLabel = "ConfigType";
        public const string DeploymentNameLabel = "Name";
        public const string DeploymentGroupIdLabel = "DeviceGroupId";
        public const string DeploymentGroupNameLabel = "DeviceGroupName";
        public const string DeploymentPackageNameLabel = "PackageName";
        public const string RmCreatedLabel = "RMDeployment";
        private const string DeviceGroupIdParameter = "deviceGroupId";
        private const string DeviceGroupQueryParameter = "deviceGroupQuery";
        private const string NameParameter = "name";
        private const string PackageContentParameter = "packageContent";
        private const string PriorityParameter = "priority";

        public DeploymentServiceModel(Configuration deployment)
        {
            if (string.IsNullOrEmpty(deployment.Id))
            {
                throw new ArgumentException($"Invalid deploymentId provided {deployment.Id}");
            }

            this.VerifyConfigurationLabel(deployment, DeploymentNameLabel);
            this.VerifyConfigurationLabel(deployment, DeploymentGroupIdLabel);
            this.VerifyConfigurationLabel(deployment, RmCreatedLabel);

            this.Id = deployment.Id;
            this.Name = deployment.Labels[DeploymentNameLabel];
            this.CreatedDateTimeUtc = deployment.CreatedTimeUtc;
            this.DeviceGroupId = deployment.Labels[DeploymentGroupIdLabel];

            if (deployment.Labels.ContainsKey(DeploymentGroupNameLabel))
            {
                this.DeviceGroupName = deployment.Labels[DeploymentGroupNameLabel];
            }

            if (deployment.Labels.ContainsKey(DeploymentPackageNameLabel))
            {
                this.PackageName = deployment.Labels[DeploymentPackageNameLabel];
            }

            this.Priority = deployment.Priority;

            if (this.IsEdgeDeployment(deployment))
            {
                this.PackageType = PackageType.EdgeManifest;
            }
            else
            {
                this.PackageType = PackageType.DeviceConfiguration;
            }

            if (deployment.Labels.ContainsKey(ConfigTypeLabel))
            {
                this.ConfigType = deployment.Labels[ConfigTypeLabel];
            }
            else
            {
                this.ConfigType = string.Empty;
            }

            this.TargetCondition = deployment.TargetCondition;

            this.DeploymentMetrics = new DeploymentMetricsServiceModel(deployment.SystemMetrics, deployment.Metrics);
            this.PackageContent = JsonConvert.SerializeObject(new PackageContent(deployment));
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

        public bool IsLatest
        {
            get { return this.Tags != null && this.Tags.Contains("reserved.latest"); }
        }

        private void VerifyConfigurationLabel(Configuration deployment, string labelName)
        {
            if (!deployment.Labels.ContainsKey(labelName))
            {
                throw new ArgumentException($"Configuration is missing necessary label {labelName}");
            }
        }

        private bool IsEdgeDeployment(Configuration deployment)
        {
            string deploymentLabel = null;

            if (deployment.Labels != null &&
                deployment.Labels.ContainsKey(PackageTypeLabel))
            {
                deploymentLabel = deployment.Labels[PackageTypeLabel];
            }

            if (!string.IsNullOrEmpty(deploymentLabel))
            {
                if (deployment.Labels.Values.Contains(PackageType.EdgeManifest.ToString()))
                {
                    return true;
                }
                else if (deployment.Labels.Values.Contains(PackageType.DeviceConfiguration.ToString()))
                {
                    return false;
                }
                else
                {
                    throw new InvalidConfigurationException("Deployment package type should not be empty.");
                }
            }
            else
            {
                /* This is for the backward compatibility, as some of the old
                 * deployments may not have the required label.
                 */
                if (deployment.Content?.ModulesContent != null)
                {
                    return true;
                }
                else if (deployment.Content?.DeviceContent != null)
                {
                    return false;
                }
                else
                {
                    throw new InvalidConfigurationException("Deployment package type should not be empty.");
                }
            }
        }
    }
}