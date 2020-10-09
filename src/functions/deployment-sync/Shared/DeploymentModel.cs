// <copyright file="DeploymentModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Microsoft.Azure.Devices;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class DeploymentModel
    {
        public string TenantId { get; set; }

        public DeploymentServiceModel Deployment { get; set; }

        public Configuration Configuration { get; set; }
    }
}