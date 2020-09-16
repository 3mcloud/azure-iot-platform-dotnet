// <copyright file="DeviceTwinName.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Mmm.Iot.Functions.Messaging.Shared
{
    public class DeviceTwinName
    {
        public HashSet<string> Tags { get; set; }

        public HashSet<string> ReportedProperties { get; set; }
    }
}