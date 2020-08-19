// <copyright file="DeploymentApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.IoTHubManager.Services;
using Mmm.Iot.IoTHubManager.Services.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mmm.Iot.IoTHubManager.WebService.Models
{
    public class DeploymentApiModel : AuditApiModel
    {
        private const string LatestTag = "reserved.latest";
        private const string InActiveTag = "reserved.inactive";

        public DeploymentApiModel()
        {
            this.Metadata = new Dictionary<string, string>
            {
                { "$type", $"DevicePropertyList;1" },
                { "$url", $"/v1/deviceproperties" },
            };
        }

        public DeploymentApiModel(DeploymentServiceModel serviceModel)
        {
            this.CreatedDateTimeUtc = serviceModel.CreatedDateTimeUtc;
            this.DeploymentId = serviceModel.Id;
            this.DeviceGroupId = serviceModel.DeviceGroupId;
            this.DeviceGroupName = serviceModel.DeviceGroupName;
            this.DeviceGroupQuery = serviceModel.DeviceGroupQuery;
            this.Name = serviceModel.Name;
            this.PackageContent = serviceModel.PackageContent;
            this.PackageName = serviceModel.PackageName;
            this.PackageId = serviceModel.PackageId;
            this.Priority = serviceModel.Priority;
            this.PackageType = serviceModel.PackageType;
            this.ConfigType = serviceModel.ConfigType;
            this.IsActive = !(serviceModel.Tags != null && serviceModel.Tags.Contains(InActiveTag));
            this.IsLatest = serviceModel.Tags != null && serviceModel.Tags.Contains(LatestTag);
            this.Metrics = new DeploymentMetricsApiModel(serviceModel.DeploymentMetrics)
            {
                DeviceStatuses = serviceModel.DeploymentMetrics?.DeviceStatuses,
            };
            this.Metadata = new Dictionary<string, string>
            {
                { "$type", $"DevicePropertyList;1" },
                { "$url", $"/v1/deviceproperties" },
            };
            this.CreatedDate = serviceModel.CreatedDate;
            this.CreatedBy = serviceModel.CreatedBy;
            this.ModifiedDate = serviceModel.ModifiedDate;
            this.ModifiedBy = serviceModel.ModifiedBy;
        }

        [JsonProperty(PropertyName = "Id")]
        public string DeploymentId { get; set; }

        [JsonProperty(PropertyName = "Name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "CreatedDateTimeUtc")]
        public DateTime CreatedDateTimeUtc { get; set; }

        [JsonProperty(PropertyName = "DeviceGroupId")]
        public string DeviceGroupId { get; set; }

        [JsonProperty(PropertyName = "DeviceGroupName")]
        public string DeviceGroupName { get; set; }

        [JsonProperty(PropertyName = "DeviceGroupQuery")]
        public string DeviceGroupQuery { get; set; }

        [JsonProperty(PropertyName = "PackageContent")]
        public string PackageContent { get; set; }

        [JsonProperty(PropertyName = "PackageName")]
        public string PackageName { get; set; }

        [JsonProperty(PropertyName = "PackageId")]
        public string PackageId { get; set; }

        [JsonProperty(PropertyName = "Priority")]
        public int Priority { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(PropertyName = "PackageType")]
        public PackageType PackageType { get; set; }

        [JsonProperty(PropertyName = "ConfigType")]
        public string ConfigType { get; set; }

        [JsonProperty(PropertyName = "Metrics", NullValueHandling = NullValueHandling.Ignore)]
        public DeploymentMetricsApiModel Metrics { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata { get; set; }

        [JsonProperty(PropertyName = "DeviceIds")]
        public IEnumerable<string> DeviceIds { get; set; }

        [JsonProperty("IsActive")]
        public bool IsActive { get; set; }

        [JsonProperty("IsLatest")]
        public bool IsLatest { get; set; }

        public DeploymentServiceModel ToServiceModel()
        {
            return new DeploymentServiceModel()
            {
                DeviceGroupId = this.DeviceGroupId,
                DeviceGroupName = this.DeviceGroupName,
                DeviceGroupQuery = this.DeviceGroupQuery,
                Name = this.Name,
                PackageContent = this.PackageContent,
                PackageId = this.PackageId,
                PackageName = this.PackageName,
                Priority = this.Priority,
                PackageType = this.PackageType,
                ConfigType = this.ConfigType,
                DeviceIds = this.DeviceIds,
            };
        }
    }
}