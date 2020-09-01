// <copyright file="IIoTHubManagementClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Management.IotHub.Models;
using Microsoft.Rest.Azure;

namespace Mmm.Iot.Common.Services.External.Azure
{
    public interface IIoTHubManagementClient : IStatusOperation
    {
        Task DeleteAsync(string iotHubName, CancellationToken token);

        Task<IotHubDescription> RetrieveAsync(string iotHubName, CancellationToken token);

        IPage<SharedAccessSignatureAuthorizationRule> ListKeysAsync(string iotHubName);

        string GetAccessKey(string iotHubName, string name);

        string GetConnectionString(string iotHubName);

        void AddConsumerGroup(string iotHubName, string endpointName, string name, string resourceGroup = null);
    }
}