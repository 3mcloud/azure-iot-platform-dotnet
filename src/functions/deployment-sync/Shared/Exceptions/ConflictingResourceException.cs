// <copyright file="ConflictingResourceException.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;

namespace Mmm.Iot.Functions.DeploymentSync.Shared.Exceptions
{
    public class ConflictingResourceException : Exception
    {
        public ConflictingResourceException()
            : base()
        {
        }

        public ConflictingResourceException(string message)
            : base(message)
        {
        }

        public ConflictingResourceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}