// <copyright file="Condition.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mmm.Iot.Common.Services.Models
{
    public class Condition
    {
        public Condition()
        {
        }

        public string Field { get; set; } = string.Empty;

        [JsonConverter(typeof(StringEnumConverter))]
        public RuleConditionOperatorType Operator { get; set; }

        public string Value { get; set; } = string.Empty;
    }
}