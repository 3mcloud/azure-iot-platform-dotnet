// <copyright file="DefaultFirmwareSettingApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

namespace Mmm.Iot.Config.WebService.Models
{
    public class DefaultFirmwareSettingApiModel
    {
        public object JsObject { get; set; }

        public DefaultFirmwareSettingMetadataModel Metadata { get; set; }
    }
}