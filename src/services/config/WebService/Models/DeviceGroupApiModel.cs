// <copyright file="DeviceGroupApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Mmm.Iot.Config.Services.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.Config.WebService.Models
{
    public class DeviceGroupApiModel
    {
        public DeviceGroupApiModel()
        {
        }

        public DeviceGroupApiModel(DeviceGroup model)
        {
            this.Id = model.Id;
            this.DisplayName = model.DisplayName;
            this.Conditions = model.Conditions;
            this.TelemetryFormat = model.TelemetryFormat;
            this.ETag = model.ETag;
            this.IsPinned = model.IsPinned;
            this.SortOrder = model.SortOrder;

            this.Metadata = new Dictionary<string, string>
            {
                { "$type", $"DeviceGroup;1" },
                { "$url", $"/v1/devicegroups/{model.Id}" },
            };
        }

        [JsonProperty("Id")]
        public string Id { get; set; }

        [JsonProperty("DisplayName")]
        public string DisplayName { get; set; }

        [JsonProperty("Conditions")]
        public IEnumerable<DeviceGroupCondition> Conditions { get; set; }

        [JsonProperty("TelemetryFormat")]
        public IEnumerable<DeviceGroupTelemetryFormat> TelemetryFormat { get; set; }

        [JsonProperty("IsPinned")]
        public bool IsPinned { get; set; }

        [JsonProperty("SortOrder")]
        public int SortOrder { get; set; }

        [JsonProperty("ETag")]
        public string ETag { get; set; }

        [JsonProperty("$metadata")]
        public Dictionary<string, string> Metadata { get; set; }

        public DeviceGroup ToServiceModel()
        {
            return new DeviceGroup
            {
                DisplayName = this.DisplayName,
                Conditions = this.Conditions,
                TelemetryFormat = this.TelemetryFormat,
                IsPinned = this.IsPinned,
                SortOrder = this.SortOrder,
            };
        }
    }
}