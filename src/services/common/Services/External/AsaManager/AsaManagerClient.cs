// <copyright file="AsaManagerClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Net.Http;
using System.Threading.Tasks;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Helpers;

namespace Mmm.Iot.Common.Services.External.AsaManager
{
    public class AsaManagerClient : ExternalServiceClient, IAsaManagerClient
    {
        public AsaManagerClient(AppConfig config, IExternalRequestHelper requestHelper)
            : base(config.ExternalDependencies.AsaManagerServiceUrl, requestHelper)
        {
        }

        public async Task<BeginConversionApiModel> BeginRulesConversionAsync()
        {
            return await this.BeginConversionAsync("rules");
        }

        public async Task<BeginConversionApiModel> BeginDeviceGroupsConversionAsync()
        {
            return await this.BeginConversionAsync("devicegroups");
        }

        public async Task<BeginConversionApiModel> BeginJobDelayDeviceGroupsConversionAsync(string jobId)
        {
            string url = $"{this.ServiceUri}/devicegroups/iothubjobdelay/{jobId}";
            return await this.RequestHelper.ProcessRequestAsync<BeginConversionApiModel>(HttpMethod.Post, url);
        }

        private async Task<BeginConversionApiModel> BeginConversionAsync(string entity)
        {
            string url = $"{this.ServiceUri}/{entity}";
            return await this.RequestHelper.ProcessRequestAsync<BeginConversionApiModel>(HttpMethod.Post, url);
        }
    }
}