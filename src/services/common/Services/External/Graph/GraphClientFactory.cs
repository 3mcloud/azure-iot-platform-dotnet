// <copyright file="GraphClientFactory.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Mmm.Iot.Common.Services.Config;

namespace Mmm.Iot.Common.Services.External.Graph
{
    public class GraphClientFactory : IGraphClientFactory
    {
        private readonly AppConfig config;

        public GraphClientFactory(AppConfig config)
        {
            this.config = config;
        }

        public GraphServiceClient Create()
        {
            // Initialize the client credential auth provider
            IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
              .Create(this.config.Global.AzureB2c.AppId) // ApplicationId from B2C
              .WithTenantId(this.config.Global.AzureB2c.TenantId) // B2C URL
              .WithClientSecret(this.config.Global.AzureB2c.AppSecret) // ClientSecret
              .Build();

            ClientCredentialProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);

            // Set up the Microsoft Graph service client with client credentials
            GraphServiceClient graphClient = new GraphServiceClient(authProvider);

            return graphClient;
        }
    }
}