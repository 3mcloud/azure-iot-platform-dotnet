// <copyright file="Audit.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class Audit
    {
        public const string DateFormat = "yyyy-MM-dd'T'HH:mm:sszzz";

        [JsonProperty(PropertyName = "CreatedDate")]
        public DateTimeOffset? CreatedDateTime { get; set; }

        [JsonProperty(PropertyName = "ModifiedDate")]
        public DateTimeOffset? ModifiedDateTime { get; set; }

        [JsonProperty(PropertyName = "CreatedBy")]
        public string CreatedBy { get; set; }

        [JsonProperty(PropertyName = "ModifiedBy")]
        public string ModifiedBy { get; set; }

        [JsonIgnore]
        public string CreatedDate => this.CreatedDateTime?.ToString(DateFormat);

        [JsonIgnore]
        public string ModifiedDate => this.ModifiedDateTime?.ToString(DateFormat);
    }
}