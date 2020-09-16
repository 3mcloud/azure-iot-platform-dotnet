// <copyright file="PackageContent.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class PackageContent
    {
        public PackageContent(Configuration configuration)
        {
            this.Id = configuration.Id;
            this.TargetCondition = configuration.TargetCondition;
            this.Priority = configuration.Priority;
            this.ETag = configuration.ETag;
            this.Content = new Content(configuration?.Content);
            this.Metrics = new Metrics(configuration?.Metrics);
        }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; }

        [JsonProperty(PropertyName = "targetCondition")]
        public string TargetCondition { get; set; }

        [JsonProperty(PropertyName = "priority")]
        public int Priority { get; set; }

        [JsonProperty(PropertyName = "etag")]
        public string ETag { get; set; }

        [JsonProperty(PropertyName = "content")]
        public Content Content { get; set; }

        [JsonProperty(PropertyName = "metrics")]
        public Metrics Metrics { get; set; }
    }
}