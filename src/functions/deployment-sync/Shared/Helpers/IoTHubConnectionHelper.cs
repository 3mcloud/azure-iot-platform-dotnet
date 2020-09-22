// <copyright file="IoTHubConnectionHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using Mmm.Iot.Functions.DeploymentSync.Shared.Exceptions;

namespace Mmm.Iot.Functions.DeploymentSync.Shared.Helpers
{
    internal class IoTHubConnectionHelper
    {
        public static void CreateUsingHubConnectionString(string hubConnString, Action<string> action)
        {
            try
            {
                action(hubConnString);
            }
            catch (ArgumentException argumentException)
            {
                // Format is not correct, for example: missing hostname
                throw new InvalidConfigurationException($"Invalid service configuration for HubConnectionstring. Exception details: {argumentException.Message}");
            }
            catch (FormatException formatException)
            {
                // SharedAccessKey is not valid base-64 string
                throw new InvalidConfigurationException($"Invalid service configuration for HubConnectionString: {hubConnString}. Exception details: {formatException.Message}");
            }
        }
    }
}