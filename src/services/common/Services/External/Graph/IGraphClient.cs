// <copyright file="IGraphClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Microsoft.Graph;

namespace Mmm.Iot.Common.Services.External.Graph
{
    public interface IGraphClient
    {
        GraphServiceClient GetClient();
    }
}