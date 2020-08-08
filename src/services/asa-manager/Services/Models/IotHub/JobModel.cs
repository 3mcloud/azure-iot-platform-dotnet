// <copyright file="JobModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using Mmm.Iot.Common.Services.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.AsaManager.Services.Models.IotHub
{
    public class JobModel
    {
        [JsonProperty("JobId")]
        public string JobId { get; set; }

        [JsonProperty(PropertyName = "CreatedTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? CreatedTimeUtc { get; set; }

        [JsonProperty(PropertyName = "StartTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? StartTimeUtc { get; set; }

        [JsonProperty(PropertyName = "EndTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? EndTimeUtc { get; set; }

        [JsonProperty(PropertyName = "MaxExecutionTimeInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        public long? MaxExecutionTimeInSeconds { get; set; }

        [JsonProperty(PropertyName = "Status")]
        public JobStatus Status { get; set; }

        [JsonProperty(PropertyName = "StatusMessage", NullValueHandling = NullValueHandling.Ignore)]
        public string StatusMessage { get; set; }
    }
}