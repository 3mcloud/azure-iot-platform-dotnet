// <copyright file="DeploymentListApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Mmm.Iot.IoTHubManager.Services.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.IoTHubManager.WebService.Models
{
    public class DeploymentListApiModel
    {
        public DeploymentListApiModel()
        {
            this.Metadata = new Dictionary<string, string>
            {
                { "$type", $"DevicePropertyList;1" },
                { "$url", $"/v1/deviceproperties" },
            };
        }

        public DeploymentListApiModel(DeploymentServiceListModel deployments)
        {
            this.Items = new List<DeploymentApiModel>();
            deployments.Items?.ForEach(deployment => this.Items.Add(new DeploymentApiModel(deployment)));

            this.Metadata = new Dictionary<string, string>
            {
                { "$type", $"DevicePropertyList;1" },
                { "$url", $"/v1/deviceproperties" },
            };
        }

        [JsonProperty(PropertyName = "Items")]
        public List<DeploymentApiModel> Items { get; set; }

        [JsonProperty(PropertyName = "$metadata")]
        public Dictionary<string, string> Metadata { get; set; }
    }
}