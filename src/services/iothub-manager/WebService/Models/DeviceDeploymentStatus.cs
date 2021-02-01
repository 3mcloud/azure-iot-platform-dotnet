// <copyright file="DeviceDeploymentStatus.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Mmm.Iot.IoTHubManager.Services;
using Mmm.Iot.IoTHubManager.Services.Models;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.IoTHubManager.WebService.Models
{
    public class DeviceDeploymentStatus
    {
        public DeviceDeploymentStatus(DeviceDeploymentStatusServiceModel deviceDeploymentStatusServiceModel)
        {
            this.Name = deviceDeploymentStatusServiceModel.Name;
            this.DeploymentStatus = deviceDeploymentStatusServiceModel.DeploymentStatus;
            this.Firmware = deviceDeploymentStatusServiceModel.Firmware;
            this.PreviousFirmware = deviceDeploymentStatusServiceModel.PreviousFirmware;
            this.Start = deviceDeploymentStatusServiceModel.Start;
            this.End = deviceDeploymentStatusServiceModel.End;
        }

        public DeviceDeploymentStatus()
        {
        }

        public string Name { get; set; }

        public string DeploymentStatus { get; set; }

        public string Firmware { get; set; }

        public string PreviousFirmware { get; set; }

        public string Start { get; set; }

        public string End { get; set; }
    }
}