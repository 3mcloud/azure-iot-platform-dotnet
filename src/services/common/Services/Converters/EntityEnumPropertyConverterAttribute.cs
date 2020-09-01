// <copyright file="EntityEnumPropertyConverterAttribute.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Cosmos.Table;

namespace Mmm.Iot.Common.Services.Converters
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EntityEnumPropertyConverterAttribute : Attribute
    {
        public EntityEnumPropertyConverterAttribute()
        {
        }
    }
}