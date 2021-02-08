// <copyright file="GraphClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Graph;
using Mmm.Iot.Common.Services.Models;

namespace Mmm.Iot.Common.Services.External.Graph
{
    public class GraphClient : IGraphClient
    {
        public GraphClient(IGraphClientFactory graphClientFactory)
        {
            this.GraphServiceClient = graphClientFactory.Create();
        }

        public GraphServiceClient GraphServiceClient { get; }

        public GraphServiceClient GetClient()
        {
            return this.GraphServiceClient;
        }

        public async Task<StatusResultServiceModel> StatusAsync()
        {
            try
            {
                var result = await this.GraphServiceClient.Users.Request().Top(1).GetAsync();
                if (result != null)
                {
                    return new StatusResultServiceModel(true, "Alive and well!");
                }
                else
                {
                    return new StatusResultServiceModel(false, $"Unable to retrieve Information from the Graph Service.");
                }
            }
            catch (Exception e)
            {
                return new StatusResultServiceModel(false, $"Graph Service status check failed: {e.Message}");
            }
        }
    }
}