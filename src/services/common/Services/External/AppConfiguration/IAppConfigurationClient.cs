// <copyright file="IAppConfigurationClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Threading.Tasks;

namespace Mmm.Iot.Common.Services.External.AppConfiguration
{
    public interface IAppConfigurationClient : IStatusOperation
    {
        Task SetValueAsync(string key, string value);

        string GetValue(string key);

        string GetValueFromAppConfig(string key);

        Task DeleteKeyAsync(string key);
    }
}