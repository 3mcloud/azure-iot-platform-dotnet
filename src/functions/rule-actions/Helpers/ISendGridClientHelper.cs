// <copyright file="ISendGridClientHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using SendGrid;

namespace Mmm.Iot.Functions.RuleActions.Helpers
{
    public interface ISendGridClientHelper
    {
        ISendGridClient GetClient();
    }
}