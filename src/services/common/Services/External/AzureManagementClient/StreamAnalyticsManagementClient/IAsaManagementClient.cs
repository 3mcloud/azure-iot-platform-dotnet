// <copyright file="IAsaManagementClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Management.IotHub.Models;
using Microsoft.Azure.Management.StreamAnalytics.Models;
using Microsoft.Rest.Azure;

namespace Mmm.Iot.Common.Services.External.Azure
{
    public interface IAsaManagementClient : IStatusOperation
    {
        Task DeleteAsync(string jobName, string resourceGroup = null, CancellationToken token = default(CancellationToken));

        Task<StreamingJob> RetrieveAsync(string jobName, string resourceGroup = null, CancellationToken token = default(CancellationToken));

        Task StopAsync(string jobName, string resourceGroup = null, CancellationToken token = default(CancellationToken));
    }
}