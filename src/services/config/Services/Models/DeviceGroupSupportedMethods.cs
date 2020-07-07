// <copyright file="DeviceGroupSupportedMethods.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mmm.Iot.Config.Services.Models
{
    public class DeviceGroupSupportedMethods
    {
        [JsonProperty("Method")]
        public string Method { get; set; }
    }
}