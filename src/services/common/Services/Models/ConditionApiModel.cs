// <copyright file="ConditionApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using Mmm.Iot.Common.Services.Exceptions;
using Newtonsoft.Json;

namespace Mmm.Iot.Common.Services.Models
{
    public class ConditionApiModel
    {
        public ConditionApiModel()
        {
        }

        public ConditionApiModel(Condition condition)
        {
            if (condition != null)
            {
                this.Field = condition.Field;
                this.Operator = condition.Operator.ToString();
                this.Value = condition.Value;
            }
        }

        [JsonProperty(PropertyName = "Field")]
        public string Field { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "Operator")]
        public string Operator { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "Value")]
        public string Value { get; set; } = string.Empty;

        public Condition ToServiceModel()
        {
            if (!Enum.TryParse<RuleConditionOperatorType>(this.Operator, true, out RuleConditionOperatorType operatorInstance))
            {
                throw new InvalidInputException("The value of 'Operator' is not valid");
            }

            return new Condition()
            {
                Field = this.Field,
                Operator = operatorInstance,
                Value = this.Value,
            };
        }
    }
}