// <copyright file="TenantOperationTypeModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Microsoft.Azure.Cosmos.Table;

namespace Mmm.Iot.TenantManager.Services.Models
{
    public enum TenantOperation
    {
        IoTHubDeletion,
        DpsDeletion,
        SaJobDeletion,
        SaJobCreation,
    }
}