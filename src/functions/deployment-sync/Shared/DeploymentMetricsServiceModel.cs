// <copyright file="DeploymentMetricsServiceModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.Azure.Devices;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class DeploymentMetricsServiceModel
    {
        public DeploymentMetricsServiceModel(
            ConfigurationMetrics systemMetrics,
            ConfigurationMetrics customMetrics)
        {
            this.SystemMetrics = systemMetrics?.Results;
            this.CustomMetrics = customMetrics?.Results;
        }

        public DeploymentMetricsServiceModel()
        {
        }

        public IDictionary<string, long> SystemMetrics { get; set; }

        public IDictionary<string, long> CustomMetrics { get; set; }

        public IDictionary<DeploymentStatus, long> DeviceMetrics { get; set; }

        public IDictionary<string, DeploymentStatus> DeviceStatuses { get; set; }
    }
}