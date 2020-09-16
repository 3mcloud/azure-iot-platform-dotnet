// <copyright file="AppConfigHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using Azure.Data.AppConfiguration;

namespace Mmm.Iot.Functions.DeploymentSync.Shared.Helpers
{
    public class AppConfigHelper
    {
        private ConfigurationClient client;

        public AppConfigHelper(string appConfigConnectionString)
        {
            this.client = new ConfigurationClient(appConfigConnectionString);
        }

        public string GetValue(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new NullReferenceException("App Config cannot take a null key parameter. The given key was not correctly configured.");
            }

            string value = string.Empty;
            try
            {
                ConfigurationSetting setting = this.client.GetConfigurationSetting(key);
                value = setting.Value;
            }
            catch (Exception e)
            {
                throw new Exception($"An exception occured while getting the value of {key} from App Config:\n" + e.Message);
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new NullReferenceException($"App Config returned a null value for {key}");
            }

            return value;
        }
    }
}