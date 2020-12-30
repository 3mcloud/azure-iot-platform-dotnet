// <copyright file="IotHubManagerClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using System.Threading.Tasks;
using Mmm.Iot.AsaManager.Services.Models.DeviceGroups;
using Mmm.Iot.AsaManager.Services.Models.IotHub;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External;
using Mmm.Iot.Common.Services.Helpers;
using Newtonsoft.Json;

namespace Mmm.Iot.AsaManager.Services.External.IotHubManager
{
    public class IotHubManagerClient : ExternalServiceClient, IIotHubManagerClient
    {
        public IotHubManagerClient(AppConfig config, IExternalRequestHelper requestHelper)
            : base(config.ExternalDependencies.IotHubManagerServiceUrl, requestHelper)
        {
        }

        public async Task<DeviceListModel> GetListAsync(IEnumerable<DeviceGroupConditionModel> conditions, string tenantId, string cToken = null)
        {
            try
            {
                var headers = new NameValueCollection();
                if (!string.IsNullOrEmpty(cToken))
                {
                    headers.Add("x-ms-continuation", cToken);
                }

                var query = JsonConvert.SerializeObject(conditions);
                var url = $"{this.ServiceUri}/devices?query={query}";
                return await this.RequestHelper.ProcessRequestAsync<DeviceListModel>(HttpMethod.Get, url, tenantId, headers);
            }
            catch (Exception e)
            {
                throw new ExternalDependencyException("Unable to get list of devices", e);
            }
        }

        public async Task<JobModel> GetJobAsync(string jobId, string tenantId)
        {
            try
            {
                var url = $"{this.ServiceUri}/jobs/{jobId}?includeDeviceDetails=false";
                return await this.RequestHelper.ProcessRequestAsync<JobModel>(HttpMethod.Get, url, tenantId);
            }
            catch (Exception e)
            {
                throw new ExternalDependencyException($"Unable to find the job with id {jobId}", e);
            }
        }
    }
}