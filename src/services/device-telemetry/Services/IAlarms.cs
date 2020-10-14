// <copyright file="IAlarms.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mmm.Iot.DeviceTelemetry.Services.Models;

namespace Mmm.Iot.DeviceTelemetry.Services
{
    public interface IAlarms
    {
        Task<Alarm> GetAsync(string id);

        Task<List<Alarm>> ListAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] devices);

        Task<List<Alarm>> ListByRuleAsync(
            string id,
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] devices);

        Task<int> GetCountByRuleAsync(
            string id,
            DateTimeOffset? from,
            DateTimeOffset? to,
            string[] devices);

        Task<Alarm> UpdateAsync(string id, string status);

        Task Delete(List<string> ids);

        Task DeleteAsync(string id);

        Task<List<Alarm>> GetAllAlarmsListByRuleAsync(
             string[] rules,
             string order,
             int skip,
             int limit);
    }
}