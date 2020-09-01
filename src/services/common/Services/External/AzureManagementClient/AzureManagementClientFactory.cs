// <copyright file="AzureManagementClientFactory.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using Microsoft.Azure.Management.IotHub;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.StreamAnalytics;
using Mmm.Iot.Common.Services.Config;

namespace Mmm.Iot.Common.Services.External.Azure
{
    public class AzureManagementClientFactory : IAzureManagementClientFactory
    {
        private readonly AppConfig config;
        private DateTime cacheExpiration;
        private AzureCredentials azureCredentials;

        public AzureManagementClientFactory(AppConfig config)
        {
            this.config = config;
            this.cacheExpiration = DateTime.UnixEpoch;
        }

        private AzureCredentials AzureCredentials
        {
            get
            {
                if (DateTime.Compare(this.cacheExpiration, DateTime.Now) < 0)
                {
                    this.cacheExpiration = DateTime.Now.AddMinutes(1);

                    var servicePrincipal = new ServicePrincipalLoginInformation();
                    servicePrincipal.ClientId = this.config.Global.AzureActiveDirectory.AppId;
                    servicePrincipal.ClientSecret = this.config.Global.AzureActiveDirectory.AppSecret;
                    this.azureCredentials = new AzureCredentials(servicePrincipal, this.config.Global.AzureActiveDirectory.TenantId, AzureEnvironment.AzureGlobalCloud);
                }

                return this.azureCredentials;
            }

            set
            {
                this.azureCredentials = value;
            }
        }

        public Microsoft.Azure.Management.Fluent.IAzure Create()
        {
            var azure = Microsoft.Azure.Management.Fluent.Azure
                .Configure()
                .Authenticate(this.AzureCredentials)
                .WithSubscription(this.config.Global.SubscriptionId);
            return azure;
        }

        public IoTHubManagementClient CreateIoTHubManagementClient()
        {
            return new IoTHubManagementClient(new IotHubClient(this.AzureCredentials), this.config);
        }

        public AsaManagementClient CreateAsaManagementClient()
        {
            return new AsaManagementClient(new StreamAnalyticsManagementClient(this.AzureCredentials), this.config);
        }
    }
}