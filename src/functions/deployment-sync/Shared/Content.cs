// <copyright file="Content.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class Content
    {
        public Content(ConfigurationContent configurationContent)
        {
            this.ModulesContent = configurationContent?.ModulesContent;
            this.DeviceContent = configurationContent?.DeviceContent;
        }

        [JsonProperty(PropertyName = "modulesContent", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, IDictionary<string, object>> ModulesContent { get; set; }

        [JsonProperty(PropertyName = "deviceContent", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, object> DeviceContent { get; set; }
    }
}