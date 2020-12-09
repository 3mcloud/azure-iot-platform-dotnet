// <copyright file="IdentityGatewayClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Helpers;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.TenantManager.Services.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.TenantManager.Services.External
{
    public class IdentityGatewayClient : IIdentityGatewayClient
    {
        private readonly IExternalRequestHelper requestHelper;
        private readonly string serviceUri;

        public IdentityGatewayClient(AppConfig config, IExternalRequestHelper requestHelper)
        {
            this.serviceUri = config.ExternalDependencies.IdentityGatewayServiceUrl;
            this.requestHelper = requestHelper;
        }

        public string RequestUrl(string path)
        {
            return $"{this.serviceUri}/{path}";
        }

        public async Task<StatusResultServiceModel> StatusAsync()
        {
            try
            {
                string url = this.RequestUrl("status/");
                var result = await this.requestHelper.ProcessRequestAsync<StatusServiceModel>(HttpMethod.Get, url);
                if (result == null || result.Status == null || !result.Status.IsHealthy)
                {
                    // bad status
                    return new StatusResultServiceModel(false, result.Status.Message);
                }
                else
                {
                    return new StatusResultServiceModel(true, "Alive and well!");
                }
            }
            catch (JsonReaderException)
            {
                return new StatusResultServiceModel(false, $"Unable to read the response from the IdentityGateway Status. The service may be down.");
            }
            catch (Exception e)
            {
                return new StatusResultServiceModel(false, $"Unable to get IdentityGateway Status: {e.Message}");
            }
        }

        public async Task<IdentityGatewayApiModel> AddTenantForUserAsync(string userId, string tenantId, string roles, string name = null)
        {
            IdentityGatewayApiModel bodyContent = new IdentityGatewayApiModel(roles, name);
            string url = this.RequestUrl($"tenants/{userId}");
            return await this.requestHelper.ProcessRequestAsync(HttpMethod.Post, url, bodyContent, tenantId);
        }

        public async Task<IdentityGatewayApiModel> GetTenantForUserAsync(string userId, string tenantId)
        {
            string url = this.RequestUrl($"tenants/{userId}");
            return await this.requestHelper.ProcessRequestAsync<IdentityGatewayApiModel>(HttpMethod.Get, url, tenantId);
        }

        public async Task<IdentityGatewayApiModel> DeleteTenantForAllUsersAsync(string tenantId)
        {
            string url = this.RequestUrl($"tenants/all");
            return await this.requestHelper.ProcessRequestAsync<IdentityGatewayApiModel>(HttpMethod.Delete, url, tenantId);
        }

        public async Task<IdentityGatewayApiSettingModel> GetSettingsForUserAsync(string userId, string settingKey)
        {
            string url = this.RequestUrl($"settings/{userId}/{settingKey}");
            return await this.requestHelper.ProcessRequestAsync<IdentityGatewayApiSettingModel>(HttpMethod.Get, url);
        }

        public async Task<IdentityGatewayApiSettingModel> AddSettingsForUserAsync(string userId, string settingKey, string settingValue)
        {
            string url = this.RequestUrl($"settings/{userId}/{settingKey}/{settingValue}");
            return await this.requestHelper.ProcessRequestAsync<IdentityGatewayApiSettingModel>(HttpMethod.Post, url);
        }

        public async Task<IdentityGatewayApiSettingModel> UpdateSettingsForUserAsync(string userId, string settingKey, string settingValue)
        {
            string url = this.RequestUrl($"settings/{userId}/{settingKey}/{settingValue}");
            return await this.requestHelper.ProcessRequestAsync<IdentityGatewayApiSettingModel>(HttpMethod.Put, url);
        }

        public async Task<IdentityGatewayApiListModel> GetAllTenantsForUserAsync(string userId)
        {
            string url = this.RequestUrl($"tenants/{userId}/all");
            return await this.requestHelper.ProcessRequestAsync<IdentityGatewayApiListModel>(HttpMethod.Get, url);
        }

        public async Task<IdentityGatewayApiListModel> GetAllSystemAdminsAsync()
        {
            string url = this.RequestUrl($"systemAdmin/getAllSystemAdmins");
            return await this.requestHelper.ProcessRequestAsync<IdentityGatewayApiListModel>(HttpMethod.Get, url);
        }
    }
}