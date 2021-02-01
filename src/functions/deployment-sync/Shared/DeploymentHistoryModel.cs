// <copyright file="DeploymentHistoryModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class DeploymentHistoryModel
    {
        public string DeploymentId { get; set; }

        public string DeviceId { get; set; }

        public string DeploymentName { get; set; }

        public TwinServiceModel PreviousFirmwareTwin { get; set; }

        public DateTime LastUpdatedDateTimeUtc { get; set; }

        public TwinServiceModel Twin { get; set; }
    }
}