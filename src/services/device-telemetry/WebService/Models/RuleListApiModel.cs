// <copyright file="RuleListApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Mmm.Iot.Common.Services.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.DeviceTelemetry.WebService.Models
{
    public class RuleListApiModel
    {
        private List<RuleApiModel> items;

        public RuleListApiModel(List<Rule> rules, bool includeDeleted)
        {
            this.items = new List<RuleApiModel>();
            if (rules != null)
            {
                foreach (Rule rule in rules)
                {
                    this.items.Add(new RuleApiModel(rule, includeDeleted));
                }
            }
        }

        public RuleListApiModel(List<Rule> rules, bool includeDeleted, Dictionary<string, string> lastTriggerValue)
        {
            this.items = new List<RuleApiModel>();
            if (rules != null)
            {
                foreach (Rule rule in rules)
                {
                    RuleApiModel ruleApi = new RuleApiModel(rule, includeDeleted);
                    ruleApi.LastTrigger = lastTriggerValue[rule.Id];
                    this.items.Add(ruleApi);
                }
            }
        }

        [JsonProperty(PropertyName = "Items")]
        public List<RuleApiModel> Items
        {
            get { return this.items; }
        }

        [JsonProperty(PropertyName = "$metadata", Order = 1000)]
        public IDictionary<string, string> Metadata => new Dictionary<string, string>
        {
            { "$type", "RuleList;1" },
            { "$uri", "/" + "v1/rules" },
        };
    }
}