// <copyright file="Rules.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.AsaManager;
using Mmm.Iot.Common.Services.External.StorageAdapter;
using Mmm.Iot.Common.Services.Helpers;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.DeviceTelemetry.Services.External;
using Mmm.Iot.DeviceTelemetry.Services.Helpers;
using Mmm.Iot.DeviceTelemetry.Services.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.DeviceTelemetry.Services
{
    public class Rules : IRules
    {
        public const string StorageCollection = "rules";
        private const string DateFormat = "yyyy-MM-dd'T'HH:mm:sszzz";
        private readonly IStorageAdapterClient storage;
        private readonly ILogger logger;
        private readonly IAsaManagerClient asaManager;
        private readonly IAlarms alarms;
        private readonly IDiagnosticsClient diagnosticsClient;

        public Rules(
            IStorageAdapterClient storage,
            IAsaManagerClient asaManager,
            ILogger<Rules> logger,
            IAlarms alarms,
            IDiagnosticsClient diagnosticsClient)
        {
            this.storage = storage;
            this.asaManager = asaManager;
            this.logger = logger;
            this.alarms = alarms;
            this.diagnosticsClient = diagnosticsClient;
        }

        public async Task CreateFromTemplateAsync(string template)
        {
            string pathToTemplate = Path.Combine(
                Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                @"Data\Rules\" + template + ".json");

            if (RuleTemplateValidator.IsValid(pathToTemplate))
            {
                var file = JToken.Parse(File.ReadAllText(pathToTemplate));

                foreach (var item in file["Rules"])
                {
                    Rule newRule = this.Deserialize(item.ToString());

                    await this.CreateAsync(newRule);
                }
            }
        }

        public async Task DeleteAsync(string id)
        {
            InputValidator.Validate(id);

            Rule existing;
            try
            {
                existing = await this.GetAsync(id);
            }
            catch (ResourceNotFoundException exception)
            {
                this.logger.LogDebug(exception, "Tried to delete rule {id} which did not exist", id);
                return;
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception, "Error trying to delete rule {id}", id);
                throw exception;
            }

            if (existing.Deleted)
            {
                return;
            }

            existing.Deleted = true;

            var item = JsonConvert.SerializeObject(existing);
            await this.storage.UpdateAsync(
                StorageCollection,
                existing.Id,
                item,
                existing.ETag);
            await this.asaManager.BeginRulesConversionAsync();
            this.LogEventAndRuleCountToDiagnostics("Rule_Deleted");
        }

        public async Task<Rule> GetAsync(string id)
        {
            InputValidator.Validate(id);

            var item = await this.storage.GetAsync(StorageCollection, id);
            var rule = this.Deserialize(item.Data);

            rule.ETag = item.ETag;
            rule.Id = item.Key;

            return rule;
        }

        public async Task<List<Rule>> GetListAsync(
            string order,
            int skip,
            int limit,
            string groupId,
            bool includeDeleted)
        {
            InputValidator.Validate(order);
            if (!string.IsNullOrEmpty(groupId))
            {
                InputValidator.Validate(groupId);
            }

            var data = await this.storage.GetAllAsync(StorageCollection);
            var ruleList = new List<Rule>();
            foreach (var item in data.Items)
            {
                try
                {
                    var rule = this.Deserialize(item.Data);
                    rule.ETag = item.ETag;
                    rule.Id = item.Key;

                    if ((string.IsNullOrEmpty(groupId) ||
                        rule.GroupId.Equals(groupId, StringComparison.OrdinalIgnoreCase))
                        && (!rule.Deleted || includeDeleted))
                    {
                        ruleList.Add(rule);
                    }
                }
                catch (Exception e)
                {
                    this.logger.LogDebug(e, "Could not parse result from Key Value Storage");
                    throw new InvalidDataException(
                        "Could not parse result from Key Value Storage", e);
                }
            }

            // sort based on MessageTime, default descending
            ruleList.Sort();

            if (order.Equals("asc", StringComparison.OrdinalIgnoreCase))
            {
                ruleList.Reverse();
            }

            if (skip >= ruleList.Count)
            {
                this.logger.LogDebug("Skip value {skip} greater than size of list returned ({ruleListCount})", skip, ruleList.Count);

                return new List<Rule>();
            }
            else if ((limit + skip) >= ruleList.Count)
            {
                // if requested values are out of range, return remaining items
                return ruleList.GetRange(skip, ruleList.Count - skip);
            }

            return ruleList.GetRange(skip, limit);
        }

        public async Task<List<AlarmCountByRule>> GetAlarmCountForListAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] devices)
        {
            InputValidator.Validate(order);
            foreach (var device in devices)
            {
                InputValidator.Validate(device);
            }

            var alarmCountByRuleList = new List<AlarmCountByRule>();

            // get list of rules
            var rulesList = await this.GetListAsync(order, skip, limit, null, true);

            // get open alarm count and most recent alarm for each rule
            foreach (var rule in rulesList)
            {
                var alarmCount = await this.alarms.GetCountByRuleAsync(
                    rule.Id,
                    from,
                    to,
                    devices);

                // skip to next rule if no alarms found
                if (alarmCount == 0)
                {
                    continue;
                }

                // get most recent alarm for rule
                var recentAlarm = await this.GetLastAlarmForRuleAsync(rule.Id, from, to, devices);

                // add alarmCountByRule to list
                alarmCountByRuleList.Add(
                    new AlarmCountByRule(
                        alarmCount,
                        recentAlarm.Status,
                        recentAlarm.DateCreated,
                        rule));
            }

            return alarmCountByRuleList;
        }

        public async Task<Dictionary<string, string>> GetLastTriggerForRules(
            List<Rule> rules)
        {
            var lastTriggeredDateList = new Dictionary<string, string>();
            List<string> ruleIds = rules.Select(x => x.Id).ToList();
            var latestAlarms = await this.GetAlarmsForRulesAsync(ruleIds);
            foreach (var rule in rules)
            {
                try
                {
                    // get most recent alarm for rule
                    var ruleAlarms = latestAlarms.Where(x => x.RuleId == rule.Id).ToList();
                    if (ruleAlarms != null && ruleAlarms.Count > 0)
                    {
                        var recentAlarm = ruleAlarms.OrderByDescending(x => x.DateModified).FirstOrDefault();
                        if (recentAlarm != null)
                        {
                            lastTriggeredDateList.Add(rule.Id, recentAlarm.DateModified.ToString());
                        }
                        else
                        {
                            lastTriggeredDateList.Add(rule.Id, null);
                        }
                    }
                    else
                    {
                        lastTriggeredDateList.Add(rule.Id, null);
                    }
                }
                catch
                {
                    lastTriggeredDateList.Add(rule.Id, null);
                }
            }

            return lastTriggeredDateList;
        }

        public async Task<Rule> CreateAsync(Rule rule)
        {
            if (rule == null)
            {
                throw new InvalidInputException("Rule not provided.");
            }

            rule.Validate();

            // Ensure dates are correct
            rule.DateCreated = DateTimeOffset.UtcNow.ToString(DateFormat);
            rule.DateModified = rule.DateCreated;

            var item = JsonConvert.SerializeObject(rule);
            var result = await this.storage.CreateAsync(StorageCollection, item);
            await this.asaManager.BeginRulesConversionAsync();

            Rule newRule = this.Deserialize(result.Data);
            newRule.ETag = result.ETag;

            if (string.IsNullOrEmpty(newRule.Id))
            {
                newRule.Id = result.Key;
            }

            this.LogEventAndRuleCountToDiagnostics("Rule_Created");
            return newRule;
        }

        public async Task<Rule> UpsertIfNotDeletedAsync(Rule rule)
        {
            rule.Validate();

            if (rule == null)
            {
                throw new InvalidInputException("Rule not provided.");
            }

            // Ensure dates are correct
            // Get the existing rule so we keep the created date correct
            Rule savedRule = null;
            try
            {
                savedRule = await this.GetAsync(rule.Id);
            }
            catch (ResourceNotFoundException e)
            {
                // Following the pattern of Post should create or update
                this.logger.LogInformation(e, "Rule not found will create new rule for ID {ruleId}", rule.Id);
            }

            if (savedRule != null && savedRule.Deleted)
            {
                throw new ResourceNotFoundException($"Rule {rule.Id} not found");
            }

            return await this.UpdateAsync(rule, savedRule);
        }

        private async void LogEventAndRuleCountToDiagnostics(string eventName)
        {
            if (this.diagnosticsClient.CanLogToDiagnostics)
            {
                // Commenting the logging of rule count as we are making extra call to fetch all the rules and
                // to decrease the burden on COSMOS DB.
                // int ruleCount = await this.GetRuleCountAsync();
                // var eventProperties = new Dictionary<string, object>
                // {
                //     { "Count", ruleCount },
                // };
                // await this.diagnosticsClient.LogEventAsync("Rule_Count", eventProperties);
                await this.diagnosticsClient.LogEventAsync(eventName);
            }
        }

        private async Task<Rule> UpdateAsync(Rule rule, Rule savedRule)
        {
            // If rule does not exist and id is provided upsert rule with that id
            if (savedRule == null && rule.Id != null)
            {
                rule.DateCreated = DateTimeOffset.UtcNow.ToString(DateFormat);
                rule.DateModified = rule.DateCreated;
            }
            else
            {
                rule.DateCreated = savedRule.DateCreated;
                rule.DateModified = DateTimeOffset.UtcNow.ToString(DateFormat);
            }

            // Save the updated rule if it exists or create new rule with id
            var item = JsonConvert.SerializeObject(rule);
            var result = await this.storage.UpdateAsync(
                StorageCollection,
                rule.Id,
                item,
                rule.ETag);
            await this.asaManager.BeginRulesConversionAsync();

            Rule updatedRule = this.Deserialize(result.Data);

            updatedRule.ETag = result.ETag;
            updatedRule.Id = result.Key;

            return updatedRule;
        }

        private async Task<Alarm> GetLastAlarmForRuleAsync(
            string id,
            DateTimeOffset? from,
            DateTimeOffset? to,
            string[] devices)
        {
            var resultList = await this.alarms.ListByRuleAsync(
                id,
                from,
                to,
                "desc",
                0,
                1,
                devices);

            if (resultList.Count != 0)
            {
                return resultList[0];
            }
            else
            {
                this.logger.LogDebug("Could not retrieve most recent alarm {id}", id);
                throw new ExternalDependencyException(
                    "Could not retrieve most recent alarm");
            }
        }

        private async Task<List<Alarm>> GetAlarmsForRulesAsync(List<string> ruleIds)
        {
            var resultList = await this.alarms.GetAllAlarmsListByRuleAsync(ruleIds.ToArray(), "desc", 0, 1000);
            return resultList;
        }

        private Rule Deserialize(string jsonRule)
        {
            try
            {
                return JsonConvert.DeserializeObject<Rule>(jsonRule);
            }
            catch (Exception e)
            {
                throw new ExternalDependencyException("Unable to parse data.", e);
            }
        }

        private async Task<int> GetRuleCountAsync()
        {
            ValueListApiModel rules = await this.storage.GetAllAsync(StorageCollection);
            int ruleCount = 0;
            foreach (var item in rules.Items)
            {
                var rule = this.Deserialize(item.Data);
                if (!rule.Deleted)
                {
                    ruleCount++;
                }
            }

            return ruleCount;
        }
    }
}