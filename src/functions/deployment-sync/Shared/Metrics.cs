// <copyright file="Metrics.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class Metrics
    {
        public Metrics(ConfigurationMetrics configurationMetrics)
        {
            this.Queries = configurationMetrics?.Queries;
        }

        [JsonProperty("queries", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> Queries { get; set; }
    }
}