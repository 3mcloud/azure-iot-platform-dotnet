// <copyright file="DeviceTwinNameOperations.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.Functions.Messaging.Shared
{
    public class DeviceTwinNameOperations
    {
        private const string WhitelistTagPrefix = "tags.";
        private const string WhitelistReportedPrefix = "reported.";
        private const string TagPrefix = "Tags.";
        private const string ReportedPrefix = "Properties.Reported.";

        public DeviceTwinName GetValidNames(string whitelist, DevicePropertyServiceModel existingData, string eventData)
        {
            ParseWhitelist(whitelist, out var fullNameWhitelist, out var prefixWhitelist);

            var validNames = new DeviceTwinName
            {
                Tags = fullNameWhitelist.Tags,
                ReportedProperties = fullNameWhitelist.ReportedProperties,
            };

            if (existingData != null && (existingData.Tags.Count > 0 || existingData.Reported.Count > 0))
            {
                validNames.Tags.UnionWith(existingData.Tags);

                validNames.ReportedProperties.UnionWith(existingData.Reported);
            }

            if (prefixWhitelist.Tags.Any() || prefixWhitelist.ReportedProperties.Any())
            {
                DeviceTwinName allNames = new DeviceTwinName();
                try
                {
                    // Get list of tags and reported properties from the twin change event data received by Event hub
                    allNames = this.GetDeviceTwinNames(eventData);
                }
                catch (Exception e)
                {
                    throw new Exception("Unable to fetch IoT devices", e);
                }

                if (allNames != null)
                {
                    validNames.Tags.UnionWith(allNames.Tags.
                        Where(s => prefixWhitelist.Tags.Any(s.StartsWith)));

                    validNames.ReportedProperties.UnionWith(
                        allNames.ReportedProperties.Where(
                            s => prefixWhitelist.ReportedProperties.Any(s.StartsWith)));
                }
            }

            return validNames;
        }

        public DeviceTwinName GetDeviceTwinNames(string eventData)
        {
            var twin = JsonConvert.DeserializeObject<Twin>(eventData);
            Dictionary<string, JToken> tags = this.TwinCollectionToDictionary(twin.Tags);
            Dictionary<string, JToken> reportedProperties = this.TwinCollectionToDictionary(twin.Properties.Reported);

            if (tags.Count > 0 || reportedProperties.Count > 0)
            {
                var tagSet = new HashSet<string>();
                var reportedSet = new HashSet<string>();
                foreach (var item in tags)
                {
                    this.PrepareTagNames(tagSet, item.Value, item.Key);
                }

                foreach (var item in reportedProperties)
                {
                    this.PrepareTagNames(reportedSet, item.Value, item.Key);
                }

                return new DeviceTwinName { Tags = tagSet, ReportedProperties = reportedSet };
            }

            return null;
        }

        private static void ParseWhitelist(
            string whitelist,
            out DeviceTwinName fullNameWhitelist,
            out DeviceTwinName prefixWhitelist)
        {
            var whitelistItems = whitelist.Split(',').Select(s => s.Trim());

            var tags = whitelistItems
                .Where(s => s.StartsWith(WhitelistTagPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(s => s.Substring(WhitelistTagPrefix.Length));

            var reported = whitelistItems
                .Where(s => s.StartsWith(WhitelistReportedPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(s => s.Substring(WhitelistReportedPrefix.Length));

            var fixedTags = tags.Where(s => !s.EndsWith("*"));

            var fixedReported = reported.Where(s => !s.EndsWith("*"));

            var regexTags = tags.Where(s => s.EndsWith("*")).Select(s => s.Substring(0, s.Length - 1));

            var regexReported = reported.
                Where(s => s.EndsWith("*")).
                Select(s => s.Substring(0, s.Length - 1));

            fullNameWhitelist = new DeviceTwinName
            {
                Tags = new HashSet<string>(fixedTags),
                ReportedProperties = new HashSet<string>(fixedReported),
            };
            prefixWhitelist = new DeviceTwinName
            {
                Tags = new HashSet<string>(regexTags),
                ReportedProperties = new HashSet<string>(regexReported),
            };
        }

        private void PrepareTagNames(HashSet<string> set, JToken jToken, string prefix)
        {
            if (jToken is JValue)
            {
                set.Add(prefix);
                return;
            }

            foreach (var item in jToken.Values())
            {
                string path = item.Path;
                this.PrepareTagNames(set, item, $"{prefix}.{(path.Contains(".") ? path.Substring(item.Path.LastIndexOf('.') + 1) : path)}");
            }
        }

        private Dictionary<string, JToken> TwinCollectionToDictionary(TwinCollection x)
        {
            var result = new Dictionary<string, JToken>();

            if (x == null)
            {
                return result;
            }

            foreach (KeyValuePair<string, object> twin in x)
            {
                try
                {
                    if (twin.Value is JToken)
                    {
                        result.Add(twin.Key, (JToken)twin.Value);
                    }
                    else
                    {
                        result.Add(twin.Key, JToken.Parse(twin.Value.ToString()));
                    }
                }
                catch (Exception ex)
                {
                    throw new ApplicationException("TwinCollectionToDictionary method failed with exception: ", ex);
                }
            }

            return result;
        }

        private TwinCollection DictionaryToTwinCollection(Dictionary<string, JToken> x)
        {
            var result = new TwinCollection();

            if (x != null)
            {
                foreach (KeyValuePair<string, JToken> item in x)
                {
                    try
                    {
                        result[item.Key] = item.Value;
                    }
                    catch (Exception ex)
                    {
                        throw new ApplicationException("DictionaryToTwinCollection method failed with exception: ", ex);
                    }
                }
            }

            return result;
        }
    }
}