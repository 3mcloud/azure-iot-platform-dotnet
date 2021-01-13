// <copyright file="DeviceTwinQueueModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class DeviceTwinQueueModel
    {
        public DeviceTwinQueueModel(string deploymentId, TwinServiceModel deviceTwin, TwinServiceModel existingTwin, string tenantId)
        {
            this.DeploymentId = deploymentId;
            this.DeviceTwin = deviceTwin;
            this.ExistingDeviceTwin = existingTwin;
            this.TenantId = tenantId;
        }

        public string DeploymentId { get; set; }

        public string TenantId { get; set; }

        public TwinServiceModel DeviceTwin { get; set; }

        public TwinServiceModel ExistingDeviceTwin { get; set; }
    }
}