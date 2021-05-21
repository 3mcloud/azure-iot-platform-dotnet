// <copyright file="UserTenantInput.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using Mmm.Iot.IdentityGateway.Services.Models;

namespace Mmm.Iot.IdentityGateway.Services
{
    public class UserTenantInput : IUserInput<UserTenantModel>
    {
        public string UserId { get; set; }

        public string Tenant { get; set; }

        public string Roles { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset? CreatedTime { get; set; }
    }
}