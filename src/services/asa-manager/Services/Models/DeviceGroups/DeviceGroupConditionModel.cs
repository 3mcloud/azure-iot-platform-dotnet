// <copyright file="DeviceGroupConditionModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Mmm.Iot.Common.Services.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mmm.Iot.AsaManager.Services.Models.DeviceGroups
{
    public class DeviceGroupConditionModel
    {
        [JsonProperty("Key")]
        public string Key { get; set; }

        [JsonProperty("Operator")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DeviceGroupConditionOperatorType Operator { get; set; }

        [JsonProperty("Value")]
        public object Value { get; set; }
    }
}