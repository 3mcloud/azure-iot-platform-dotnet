// <copyright file="IDpsManagementClient.cs" company="3M">
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
    public interface IDpsManagementClient : IStatusOperation
    {
        Task DeleteAsync(string dpsName, string resourceGroup = null);

        Task<bool> Exists(string dpsName, string resourceGroup = null);
    }
}