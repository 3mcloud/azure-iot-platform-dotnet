// <copyright file="TenantModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Microsoft.Azure.Cosmos.Table;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class TenantModel : TableEntity
    {
        public TenantModel()
        {
        }

        public string TenantId { get; set; }

        public string IotHubName { get; set; }

        public string SAJobName { get; set; }

        public bool IsIotHubDeployed { get; set; }

        public string TenantName { get; set; }
    }
}