// <copyright file="TenantOperationModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.Azure.Cosmos.Table;
using Mmm.Iot.Common.Services.Converters;

namespace Mmm.Iot.TenantManager.Services.Models
{
    public class TenantOperationModel : TableEntity
    {
        public TenantOperationModel()
        {
        }

        public TenantOperationModel(string id, TenantOperation operation, string name = "")
        {
            // Use the first character of the tenant id as the partion key as it is randomly distributed
            this.PartitionKey = id;
            this.RowKey = operation.ToString();
            this.TenantId = id;
            this.Type = operation;
            this.Name = name;
        }

        public string Name { get; set; }

        public string TenantId { get; set; }

        [EntityEnumPropertyConverter]
        public TenantOperation Type { get; set; }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);
            EntityEnumPropertyConverter.Deserialize(this, properties);
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var results = base.WriteEntity(operationContext);
            EntityEnumPropertyConverter.Serialize(this, results);
            return results;
        }
    }
}