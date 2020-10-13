// <copyright file="IRules.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mmm.Iot.Common.Services.Models;

namespace Mmm.Iot.DeviceTelemetry.Services
{
    public interface IRules
    {
        Task CreateFromTemplateAsync(string template);

        Task DeleteAsync(string id);

        Task<Rule> GetAsync(string id);

        Task<List<Rule>> GetListAsync(
            string order,
            int skip,
            int limit,
            string groupId,
            bool includeDeleted);

        Task<List<AlarmCountByRule>> GetAlarmCountForListAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] devices);

        Task<Rule> CreateAsync(Rule rule);

        Task<Rule> UpsertIfNotDeletedAsync(Rule rule);

        Task<Dictionary<string, string>> GetLastTriggerForRules(
             List<Rule> rules);
    }
}