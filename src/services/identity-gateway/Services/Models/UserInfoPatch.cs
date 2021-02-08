// <copyright file="UserInfoPatch.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Mmm.Iot.IdentityGateway.Services.Models
{
    public class UserInfoPatch
    {
        public UserInfoPatch()
        {
            this.AlternateEmails = new List<string>();
        }

        public string MobilePhone { get; set; }

        public string StreetAddress { get; set; }

        public string City { get; set; }

        public string PostalCode { get; set; }

        public List<string> AlternateEmails { get; set; }
    }
}