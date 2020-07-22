// <copyright file="IBeginConversionHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Mmm.Iot.AsaManager.Services.Models;
using Mmm.Iot.AsaManager.Services.Models.IotHub;

namespace Mmm.Iot.AsaManager.WebService.Helpers
{
    public delegate Task<ConversionApiModel> ConvertAsync(string tenantId, string operationId);

    public interface IBeginConversionHelper
    {
        string BeginConversion(ConvertAsync convertAsync, string tenantId);

        string WatchIotHubJobAndBeginConversion(string jobId, ConvertAsync convertAsync, string tenantId);
    }
}