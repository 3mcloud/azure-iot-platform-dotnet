// <copyright file="DeviceReportServiceModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Mmm.Iot.Common.Services.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class DeviceReportServiceModel
    {
        public const string DateFormat = "yyyy-MM-dd'T'HH:mm:sszzz";

        public DeviceReportServiceModel(DeviceServiceModel deviceServiceModel)
        {
            this.NormalizedReportedProperties(deviceServiceModel?.Twin?.ReportedProperties);
            this.DeviceName = deviceServiceModel.Id;
            this.LastActivity = deviceServiceModel.LastActivity.ToString(DateFormat);
            this.Connected = deviceServiceModel.Connected;
            this.DeviceType = this.GetValue("type");
            this.Firmware = this.GetValue("firmware.currentFwVersion");
            this.Telemetry = this.GetValue(deviceServiceModel?.Twin?.ReportedProperties, "telemetry");
            this.IsSimulated = deviceServiceModel.Twin.IsSimulated;
        }

        public string DeviceName { get; set; }

        public bool IsSimulated { get; set; }

        public string DeviceType { get; set; }

        public string LastActivity { get; set; }

        public bool Connected { get; set; }

        public string Firmware { get; set; }

        public string Telemetry { get; set; }

        public Dictionary<string, JValue> ReportedProperties { get; set; }

        private string GetValue(string key)
        {
            string value = string.Empty;

            if (this.ReportedProperties.ContainsKey(key))
            {
                value = this.ReportedProperties[key].Value<string>();
            }

            return value;
        }

        private string GetValue(Dictionary<string, JToken> reportedProperties, string key)
        {
            string value = string.Empty;

            if (reportedProperties != null && reportedProperties.ContainsKey(key))
            {
                if (reportedProperties[key].Type == JTokenType.Object)
                {
                    value = reportedProperties[key].ToString(Formatting.None);
                }
            }

            return value;
        }

        private void NormalizedReportedProperties(Dictionary<string, JToken> reportedProperties)
        {
            var json = JToken.Parse(JsonConvert.SerializeObject(reportedProperties));
            var fieldsCollector = new JsonFieldsCollector(json);
            this.ReportedProperties = fieldsCollector.GetAllFields();
        }
    }
}