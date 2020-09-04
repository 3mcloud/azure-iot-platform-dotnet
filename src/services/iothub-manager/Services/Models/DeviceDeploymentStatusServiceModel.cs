// <copyright file="DeviceDeploymentStatusServiceModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Mmm.Iot.Common.Services.Helpers;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class DeviceDeploymentStatusServiceModel
    {
        public const string DateFormat = "yyyy-MM-dd'T'HH:mm:sszzz";

        public DeviceDeploymentStatusServiceModel(string deviceId, DeploymentStatus deploymentStatus, Dictionary<string, JValue> reportedProperties)
        {
            this.Name = deviceId;
            this.DeploymentStatus = deploymentStatus.ToString();
            this.Firmware = reportedProperties.ContainsKey("firmware.currentFwVersion") ? reportedProperties["firmware.currentFwVersion"].Value<string>() : string.Empty;
            this.Start = reportedProperties.ContainsKey("firmware.lastFwUpdateStartTime") ? DateTimeHelper.FormatDate(reportedProperties["firmware.lastFwUpdateStartTime"], DateFormat) : string.Empty;
            this.End = reportedProperties.ContainsKey("firmware.lastFwUpdateEndTime") ? DateTimeHelper.FormatDate(reportedProperties["firmware.lastFwUpdateEndTime"], DateFormat) : string.Empty;
        }

        public DeviceDeploymentStatusServiceModel()
        {
        }

        public string Name { get; set; }

        public string DeploymentStatus { get; set; }

        public string Firmware { get; set; }

        public string Start { get; set; }

        public string End { get; set; }
    }
}