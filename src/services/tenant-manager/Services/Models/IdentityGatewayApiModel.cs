// <copyright file="IdentityGatewayApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Mmm.Iot.TenantManager.Services.Models
{
    public class IdentityGatewayApiModel
    {
        public IdentityGatewayApiModel()
        {
        }

        public IdentityGatewayApiModel(string roles)
        {
            this.Roles = roles;
        }

        public IdentityGatewayApiModel(string roles, string name)
        {
            this.Roles = roles;
            this.Name = name;
        }

        public string Roles { get; set; }

        public string UserId { get; set; }

        public string TenantId { get; set; }

        public string PartitionKey { get; set; }

        public string Name { get; set; }

        public string RowKey { get; set; }

        public List<string> RoleList
        {
            get
            {
                try
                {
                    return JsonConvert.DeserializeObject<List<string>>(this.Roles);
                }
                catch
                {
                    return new List<string>(); // cant Deserialize return Empty List
                }
            }
        }
    }
}