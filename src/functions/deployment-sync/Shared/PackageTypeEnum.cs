// <copyright file="PackageTypeEnum.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public enum PackageType
    {
        EdgeManifest,
        DeviceConfiguration,
    }
}