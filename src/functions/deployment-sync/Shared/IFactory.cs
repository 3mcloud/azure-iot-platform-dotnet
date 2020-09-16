// <copyright file="IFactory.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public interface IFactory<out T>
    {
        T Create(string dBConnectionString);
    }
}