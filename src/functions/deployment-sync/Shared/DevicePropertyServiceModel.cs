// <copyright file="DevicePropertyServiceModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class DevicePropertyServiceModel
    {
        public bool Rebuilding { get; set; } = false;

        public HashSet<string> Tags { get; set; }

        public HashSet<string> Reported { get; set; }

        public bool IsNullOrEmpty() => (this.Tags == null || this.Tags.Count == 0) && (this.Reported == null || this.Reported.Count == 0);
    }
}