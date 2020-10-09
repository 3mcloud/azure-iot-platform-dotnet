// <copyright file="ConfigurationsHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Microsoft.Azure.Devices;
using Mmm.Iot.Functions.DeploymentSync.Shared.Exceptions;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public static class ConfigurationsHelper
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

        public static bool IsEdgeDeployment(Configuration deployment)
        {
            string deploymentLabel = null;

            if (deployment.Labels != null &&
                deployment.Labels.ContainsKey(ConfigurationsHelper.PackageTypeLabel))
            {
                deploymentLabel = deployment.Labels[ConfigurationsHelper.PackageTypeLabel];
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