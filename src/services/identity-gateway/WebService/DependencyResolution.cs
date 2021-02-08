// <copyright file="DependencyResolution.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Reflection;
using Autofac;
using Mmm.Iot.Common.Services;
using Mmm.Iot.IdentityGateway.Services;

namespace Mmm.Iot.IdentityGateway.WebService
{
    public class DependencyResolution : DependencyResolutionBase
    {
        protected override void SetupCustomRules(ContainerBuilder builder)
        {
            var assembly = typeof(StatusService).GetTypeInfo().Assembly;
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();
            builder.RegisterType<UserSettingsContainer>().SingleInstance();
            builder.RegisterType<UserTenantContainer>().SingleInstance();
            builder.RegisterType<SystemAdminContainer>().SingleInstance();
            builder.RegisterType<AuthorizeContainer>().SingleInstance();
            builder.RegisterType<GraphUserContainer>().SingleInstance();
            /*builder.RegisterType<ADB2CConnect>().As<IADB2CConnect>().SingleInstance();
            builder.RegisterType<Auth0Connect>().As<IAuth0Connect>().SingleInstance();*/
        }
    }
}