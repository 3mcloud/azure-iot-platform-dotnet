// <copyright file="Startup.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Threading;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Mmm.Iot.Common.Services.AIPreprocessors;
using Mmm.Iot.Common.Services.Auth;
using Mmm.Iot.Common.Services.Config;

namespace Mmm.Iot.DeviceTelemetry.WebService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public IContainer ApplicationContainer { get; private set; }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc($"v1", new OpenApiInfo { Title = "Device Telemetry API", Version = "v1" });
            });
            var applicationInsightsOptions = new Microsoft.ApplicationInsights.AspNetCore.Extensions.ApplicationInsightsServiceOptions();
            applicationInsightsOptions.EnableAdaptiveSampling = false;
            services.AddApplicationInsightsTelemetry(applicationInsightsOptions);
            services.AddApplicationInsightsTelemetryProcessor<HealthProbeTelemetryProcessor>();
            services.AddMvc().AddControllersAsServices().AddNewtonsoftJson();
            services.AddHttpContextAccessor();
            this.ApplicationContainer = new DependencyResolution().Setup(services, this.Configuration);
            return new AutofacServiceProvider(this.ApplicationContainer);
        }

        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            IHostApplicationLifetime appLifetime,
            AppConfig config)
        {
            app.UseRouting();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("./swagger/v1/swagger.json", "V1");
                c.RoutePrefix = string.Empty;
            });
            SetupTelemetry(app, config);
            app.UseMiddleware<AuthMiddleware>();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            appLifetime.ApplicationStopped.Register(() => this.ApplicationContainer.Dispose());
        }

        private static void SetupTelemetry(IApplicationBuilder app, AppConfig config)
        {
            var configuration = app.ApplicationServices.GetService<TelemetryConfiguration>();
            var builder = configuration.DefaultTelemetrySink.TelemetryProcessorChainBuilder;

            // Using fixed rate sampling
            double fixedSamplingPercentage = config.Global.FixedSamplingPercentage == 0 ? 10 : config.Global.FixedSamplingPercentage;
            builder.UseSampling(fixedSamplingPercentage);
            builder.Use(next => new HealthProbeTelemetryProcessor(next));
            builder.Build();
        }
    }
}