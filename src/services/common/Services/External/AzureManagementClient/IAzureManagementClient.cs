// <copyright file="IAzureManagementClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Management.IotHub.Models;
using Microsoft.Rest.Azure;
using Mmm.Iot.Common.Services.External.TableStorage;

namespace Mmm.Iot.Common.Services.External.Azure
{
    public interface IAzureManagementClient : IStatusOperation
    {
        IoTHubManagementClient IotHubManagementClient { get; }

        DpsManagementClient DpsManagmentClient { get; }

        AsaManagementClient AsaManagementClient { get; }

        Task DeployTemplateAsync(string template, string resourceGroup = null, string deploymentName = null);
    }
}