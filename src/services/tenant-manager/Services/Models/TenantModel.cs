// <copyright file="TenantModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Microsoft.Azure.Cosmos.Table;

namespace Mmm.Iot.TenantManager.Services.Models
{
    public class TenantModel : TableEntity
    {
        public TenantModel()
        {
        }

        public TenantModel(string id)
        {
            // Use the first character of the tenant id as the partion key as it is randomly distributed
            this.PartitionKey = id.Substring(0, 1);
            this.RowKey = id;
            this.TenantId = id;
            this.IotHubName = string.Empty;
            this.DpsName = string.Empty;
            this.SAJobName = string.Empty;
            this.IsIotHubDeployed = false;
            this.MarkedForDeletion = false;
            this.TenantName = string.Empty;
        }

        public string TenantId { get; set; }

        public string IotHubName { get; set; }

        public string DpsName { get; set; }

        public string SAJobName { get; set; }

        public bool IsIotHubDeployed { get; set; }

        public bool MarkedForDeletion { get; set; }

        public string TenantName { get; set; }
    }
}