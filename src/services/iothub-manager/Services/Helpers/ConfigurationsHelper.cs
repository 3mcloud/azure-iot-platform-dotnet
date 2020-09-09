// <copyright file="ConfigurationsHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Azure.Devices;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.IoTHubManager.Services.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.IoTHubManager.Services.Helpers
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

        public static Configuration ToHubConfiguration(DeploymentServiceModel model)
        {
            var packageConfiguration = JsonConvert.DeserializeObject<Configuration>(model.PackageContent);

            if (model.PackageType.Equals(PackageType.EdgeManifest) &&
                packageConfiguration.Content?.DeviceContent != null)
            {
                throw new InvalidInputException("Deployment type does not match with package contents.");
            }
            else if (model.PackageType.Equals(PackageType.DeviceConfiguration) &&
                packageConfiguration.Content?.ModulesContent != null)
            {
                throw new InvalidInputException("Deployment type does not match with package contents.");
            }

            var deploymentId = Guid.NewGuid().ToString().ToLower();
            var configuration = new Configuration(deploymentId);
            configuration.Content = packageConfiguration.Content;

            // Model.TargetCondition will not be empty when synced from IoTHub to Cosmos, which will be used
            // while reactivating the deployment
            if (string.IsNullOrWhiteSpace(model.TargetCondition))
            {
                var targetCondition = QueryConditionTranslator.ToQueryString(model.DeviceGroupQuery);
                if (model.DeviceIds != null && model.DeviceIds.Any())
                {
                    string deviceIdCondition = $"({string.Join(" or ", model.DeviceIds.Select(v => $"deviceId = '{v}'"))})";
                    if (!string.IsNullOrWhiteSpace(targetCondition))
                    {
                        string[] conditions = { targetCondition, deviceIdCondition };
                        targetCondition = string.Join(" or ", conditions);
                    }
                    else
                    {
                        targetCondition = deviceIdCondition;
                    }
                }

                configuration.TargetCondition = string.IsNullOrEmpty(targetCondition) ? "*" : targetCondition;
            }
            else
            {
                configuration.TargetCondition = model.TargetCondition;
            }

            configuration.Priority = model.Priority;
            configuration.ETag = string.Empty;
            configuration.Labels = packageConfiguration.Labels ?? new Dictionary<string, string>();

            // Required labels
            configuration.Labels[PackageTypeLabel] = model.PackageType.ToString();
            configuration.Labels[DeploymentNameLabel] = model.Name;
            configuration.Labels[DeploymentGroupIdLabel] = model.DeviceGroupId;
            configuration.Labels[RmCreatedLabel] = bool.TrueString;
            if (!string.IsNullOrEmpty(model.ConfigType))
            {
                configuration.Labels[ConfigTypeLabel] = model.ConfigType;
            }

            var customMetrics = packageConfiguration.Metrics?.Queries;
            if (customMetrics != null)
            {
                configuration.Metrics.Queries = SubstituteDeploymentIdIfPresent(
                                                                    customMetrics,
                                                                    deploymentId);
            }

            // Add optional labels
            if (model.DeviceGroupName != null)
            {
                configuration.Labels[DeploymentGroupNameLabel] = model.DeviceGroupName;
            }

            if (model.PackageName != null)
            {
                configuration.Labels[DeploymentPackageNameLabel] = model.PackageName;
            }

            return configuration;
        }

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

        // Replaces DeploymentId, if present, in the custom metrics query
        public static IDictionary<string, string> SubstituteDeploymentIdIfPresent(
            IDictionary<string, string> customMetrics,
            string deploymentId)
        {
            const string deploymentClause = @"configurations\.\[\[.+\]\]"; // replace all configuration clauses in custom metrics
            string updatedDeploymentClause = $"configurations.[[{deploymentId}]]";
            IDictionary<string, string> metrics = new Dictionary<string, string>();

            foreach (KeyValuePair<string, string> query in customMetrics)
            {
                metrics[query.Key] = Regex.Replace(query.Value, deploymentClause, updatedDeploymentClause);
            }

            return metrics;
        }
    }
}