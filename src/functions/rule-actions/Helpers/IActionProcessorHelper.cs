// <copyright file="IActionProcessorHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Mmm.Iot.Functions.RuleActions.Models;

namespace Mmm.Iot.Functions.RuleActions.Helpers
{
    public interface IActionProcessorHelper
    {
        Task TriggerActions(ExecutionContext context, ActionEvent actionEvent);
    }
}