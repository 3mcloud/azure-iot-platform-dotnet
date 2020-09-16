// <copyright file="IFactory.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

namespace Mmm.Iot.Functions.Messaging.Shared
{
    public interface IFactory<out T>
    {
        T Create(string dBConnectionString);
    }
}