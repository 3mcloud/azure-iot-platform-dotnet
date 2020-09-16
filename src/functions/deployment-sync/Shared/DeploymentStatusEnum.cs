// <copyright file="DeploymentStatusEnum.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DeploymentStatus
    {
        Pending,
        Succeeded,
        Failed,
        Unknown,
    }
}