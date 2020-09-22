// <copyright file="TenantQueueItem.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class TenantQueueItem
    {
        public TenantQueueItem(string tenantId)
        {
            this.TenantId = tenantId;
        }

        public string TenantId { get; set; }
    }
}