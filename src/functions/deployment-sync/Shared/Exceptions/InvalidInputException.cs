// <copyright file="InvalidInputException.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;

namespace Mmm.Iot.Functions.DeploymentSync.Shared.Exceptions
{
    public class InvalidInputException : Exception
    {
        public InvalidInputException()
            : base()
        {
        }

        public InvalidInputException(string message)
            : base(message)
        {
        }

        public InvalidInputException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}