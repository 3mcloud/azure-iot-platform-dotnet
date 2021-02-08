// <copyright file="AppConfigurationClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.AppConfiguration;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Models;

namespace Mmm.Iot.Common.Services.External.AppConfiguration
{
    public class AppConfigurationClient : IAppConfigurationClient
    {
        private readonly AppConfig config;
        private ConfigurationClient client;
        private Dictionary<string, AppConfigCacheValue> cache = new Dictionary<string, AppConfigCacheValue>();

        public AppConfigurationClient(AppConfig config)
        {
            this.client = new ConfigurationClient(config.AppConfigurationConnectionString);
            this.config = config;
        }

        public AppConfigurationClient(AppConfig config, IConfigurationClientFactory clientFactory)
        {
            this.client = clientFactory.Create();
            this.config = config;
        }

        public async Task<StatusResultServiceModel> StatusAsync()
        {
            // Test a key created from one of the configuration variable names
            string statusKey = string.Empty;
            try
            {
                string configKey = this.config.Global
                    .GetType()
                    .GetProperties()
                    .First(prop => prop.PropertyType == typeof(string))
                    .Name;

                // Append global to the retrieved configKey and append global to it, since that is its parent key name
                statusKey = $"Global:{configKey}";
            }
            catch (Exception)
            {
                return new StatusResultServiceModel(false, $"Unable to get the status of AppConfig because no Global Configuration Key could be retrieved from the generated configuration class.");
            }

            try
            {
                await Task.FromResult(this.GetValue(statusKey));
                return new StatusResultServiceModel(true, "Alive and well!");
            }
            catch (Exception e)
            {
                return new StatusResultServiceModel(false, $"Unable to retrieve a key from AppConfig. {e.Message}");
            }
        }

        public async Task SetValueAsync(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("the key parameter must not be null or empty to create a new app config key value pair.");
            }

            try
            {
                await this.client.SetConfigurationSettingAsync(key, value);
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to create the app config key value pair {{{key}, {value}}}", e);
            }
        }

        public string GetValue(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("App Config cannot take a null key parameter. The given key was not correctly configured.");
            }

            string value = string.Empty;
            try
            {
                if (this.cache.ContainsKey(key) && this.cache[key].ExpirationTime > DateTime.UtcNow)
                {
                    value = this.cache[key].Value.Value; // get string from configuration setting
                }
                else
                {
                    ConfigurationSetting setting = this.client.GetConfigurationSetting(key);
                    value = setting.Value;
                    if (this.cache.ContainsKey(key))
                    {
                        this.cache[key] = new AppConfigCacheValue(setting);
                    }
                    else
                    {
                        this.cache.Add(key, new AppConfigCacheValue(setting));
                    }
                }
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

        public string GetValueFromAppConfig(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("App Config cannot take a null key parameter. The given key was not correctly configured.");
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

        public async Task DeleteKeyAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("The key parameter must not be null or empty to delete an app config key value pair.");
            }

            try
            {
                await this.client.DeleteConfigurationSettingAsync(key);
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to delete the app config key value pair for key {key}", e);
            }
        }
    }
}