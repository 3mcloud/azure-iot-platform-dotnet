// <copyright file="WebHost.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Mmm.Iot.Common.Services.Config;

namespace Mmm.Iot.Common.Services
{
    public class WebHost
    {
        private static AppConfig azureAppConfig;

        public static IWebHostBuilder CreateDefaultBuilder(string[] args)
        {
            var builder = new WebHostBuilder();
            builder.UseEnvironment(EnvironmentName.Development);
            UseContentRoot(builder);
            ConfigureHostConfiguration(args, builder);
            ConfigureAppConfiguration(args, builder);
            ConfigureLogging(builder);
            builder.UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateScopes = context.HostingEnvironment.EnvironmentName == EnvironmentName.Development;
            });
            builder.UseKestrel(options =>
            {
                options.AddServerHeader = false;
                options.Limits.MaxRequestBodySize = azureAppConfig.Global.Limits.FileUploadLimit;

            });
            builder.UseIISIntegration();
            return builder;
        }

        private static void ConfigureLogging(WebHostBuilder builder)
        {
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.AddApplicationInsights(azureAppConfig.Global.InstrumentationKey);
                logging.SetMinimumLevel(LogLevel.Debug);

                // The following configures LogLevel Information or above to be sent to Application Insights for all categories.
                logging.AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Information);
                logging.AddFilter("System", LogLevel.Warning);
                logging.AddFilter("Microsoft", LogLevel.Warning);
            });
        }

        private static void ConfigureAppConfiguration(string[] args, WebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;
                var configurationBuilder = new ConfigurationBuilder();

                if (env.EnvironmentName == EnvironmentName.Development || env.EnvironmentName == EnvironmentName.Qa)
                {
                    var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                    if (appAssembly != null)
                    {
                        configurationBuilder.AddUserSecrets(appAssembly, optional: true);
                    }
                }

                configurationBuilder.AddEnvironmentVariables();
                if (args != null)
                {
                    configurationBuilder.AddCommandLine(args);
                }

                var initialAppConfig = new AppConfig(configurationBuilder);
                configurationBuilder = new ConfigurationBuilder();
                configurationBuilder.AddAzureAppConfiguration(initialAppConfig.AppConfigurationConnectionString);
                var azureAppConfigConfig = new AppConfig(configurationBuilder);
                config.AddConfiguration(azureAppConfigConfig.Configuration);
                config.AddAzureKeyVault(
                    $"https://{azureAppConfigConfig.Global.KeyVault.Name}.vault.azure.net/",
                    azureAppConfigConfig.Global.AzureActiveDirectory.AppId,
                    azureAppConfigConfig.Global.AzureActiveDirectory.AppSecret);
                config.AddConfiguration(initialAppConfig.Configuration);
                azureAppConfig = azureAppConfigConfig;
            });
        }

        private static void ConfigureHostConfiguration(string[] args, WebHostBuilder builder)
        {
            var hostConfigurationBuilder = new ConfigurationBuilder();
            hostConfigurationBuilder.AddEnvironmentVariables();
            if (args != null)
            {
                hostConfigurationBuilder.AddCommandLine(args);
            }

            builder.UseConfiguration(hostConfigurationBuilder.Build());
        }

        private static void UseContentRoot(WebHostBuilder builder)
        {
            if (string.IsNullOrEmpty(builder.GetSetting(WebHostDefaults.ContentRootKey)))
            {
                builder.UseContentRoot(Directory.GetCurrentDirectory());
            }
        }
    }
}