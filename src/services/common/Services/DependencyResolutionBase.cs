// <copyright file="DependencyResolutionBase.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Mmm.Iot.Common.Services.Auth;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.External.AppConfiguration;
using Mmm.Iot.Common.Services.External.AsaManager;
using Mmm.Iot.Common.Services.External.Azure;
using Mmm.Iot.Common.Services.External.BlobStorage;
using Mmm.Iot.Common.Services.External.CosmosDb;
using Mmm.Iot.Common.Services.External.StorageAdapter;
using Mmm.Iot.Common.Services.External.TableStorage;
using Mmm.Iot.Common.Services.External.TimeSeries;
using Mmm.Iot.Common.Services.External.UserManagement;
using Mmm.Iot.Common.Services.Helpers;
using Mmm.Iot.Common.Services.Http;
using Mmm.Iot.Common.Services.Wrappers;

namespace Mmm.Iot.Common.Services
{
    public abstract class DependencyResolutionBase
    {
        public IContainer Setup(IServiceCollection services, IConfiguration configuration)
        {
            var builder = new ContainerBuilder();
            var appConfig = new AppConfig();
            configuration.Bind(appConfig);
            builder.RegisterInstance(appConfig).SingleInstance();
            var applicationInsightHelper = new ApplicationInsightsHelper();
            configuration.Bind(applicationInsightHelper);
            builder.RegisterInstance(applicationInsightHelper).SingleInstance();
            builder.Populate(services);
            builder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>));
            this.AutowireAssemblies(builder);
            builder.RegisterType<UserManagementClient>().As<IUserManagementClient>().SingleInstance();
            builder.RegisterType<AppConfigurationClient>().As<IAppConfigurationClient>().SingleInstance();
            builder.RegisterType<StorageAdapterClient>().As<IStorageAdapterClient>().SingleInstance();
            builder.RegisterType<ExternalRequestHelper>().As<IExternalRequestHelper>().SingleInstance();
            builder.RegisterType<CustomEventLogHelper>().As<ICustomEventLogHelper>().SingleInstance();
            builder.RegisterType<GuidKeyGenerator>().As<IKeyGenerator>().SingleInstance();
            builder.RegisterType<HttpClient>().As<IHttpClient>().SingleInstance();
            builder.Register(context => GetOpenIdConnectManager(context.Resolve<AppConfig>())).As<IConfigurationManager<OpenIdConnectConfiguration>>().SingleInstance();
            builder.RegisterType<CorsSetup>().As<ICorsSetup>().SingleInstance();
            builder.RegisterType<StorageClient>().As<IStorageClient>().SingleInstance();
            builder.RegisterType<AsaManagerClient>().As<IAsaManagerClient>().SingleInstance();
            builder.RegisterType<TimeSeriesClient>().As<ITimeSeriesClient>().SingleInstance();
            builder.RegisterType<CloudTableClientFactory>().As<ICloudTableClientFactory>().SingleInstance();
            builder.RegisterType<TableStorageClient>().As<ITableStorageClient>().SingleInstance();
            builder.RegisterType<AzureManagementClientFactory>().As<IAzureManagementClientFactory>().SingleInstance();
            builder.RegisterType<AzureManagementClient>().As<IAzureManagementClient>().SingleInstance();
            builder.RegisterType<BlobStorageClient>().As<IBlobStorageClient>().SingleInstance();
            this.SetupCustomRules(builder);
            var container = builder.Build();
            Factory.RegisterContainer(container);
            return container;
        }

        protected abstract void SetupCustomRules(ContainerBuilder builder);

        // Prepare the OpenId Connect configuration manager, responsibile
        // for retrieving the JWT signing keys and cache them in memory.
        // See: https://openid.net/specs/openid-connect-discovery-1_0.html#rfc.section.4
        private static IConfigurationManager<OpenIdConnectConfiguration> GetOpenIdConnectManager(AppConfig config)
        {
            // Avoid starting the real OpenId Connect manager if not needed, which would
            // start throwing errors when attempting to fetch certificates.
            if (!config.Global.AuthRequired)
            {
                return new StaticConfigurationManager<OpenIdConnectConfiguration>(
                    new OpenIdConnectConfiguration());
            }

            var clientHandler = new System.Net.Http.HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (a, b, c, d) => true;
            var client = new System.Net.Http.HttpClient(clientHandler);

            return new ConfigurationManager<OpenIdConnectConfiguration>(
                config.Global.ClientAuth.Jwt.AuthIssuer + "/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                client)
            {
                // How often the list of keys in memory is refreshed. Default is 24 hours.
                AutomaticRefreshInterval = TimeSpan.FromHours(6),

                // The minimum time between retrievals, in the event that a retrieval
                // failed, or that a refresh is explicitly requested. Default is 30 seconds.
                RefreshInterval = TimeSpan.FromMinutes(1),
            };
        }

        private void AutowireAssemblies(ContainerBuilder builder)
        {
            var assembly = Assembly.GetEntryAssembly();
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();
        }
    }
}