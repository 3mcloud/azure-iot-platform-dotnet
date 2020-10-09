// <copyright file="InvalidConfigurationException.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;

namespace Mmm.Iot.Functions.DeploymentSync.Shared.Exceptions
{
    public class InvalidConfigurationException : Exception
    {
        public InvalidConfigurationException()
            : base()
        {
        }

        public InvalidConfigurationException(string message)
            : base(message)
        {
        }

        public InvalidConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}