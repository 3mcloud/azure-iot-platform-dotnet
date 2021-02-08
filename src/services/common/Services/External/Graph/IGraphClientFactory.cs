// <copyright file="IGraphClientFactory.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Microsoft.Graph;

namespace Mmm.Iot.Common.Services.External.Graph
{
    public interface IGraphClientFactory
    {
        GraphServiceClient Create();
    }
}