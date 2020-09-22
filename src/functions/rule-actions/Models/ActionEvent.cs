// <copyright file="ActionEvent.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Mmm.Iot.Functions.RuleActions.Models
{
    public class ActionEvent
    {
        public string RuleDescription { get; set; }

        public string RuleSeverity { get; set; }

        public string RuleId { get; set; }

        public List<RuleAction> RuleActions { get; set; }

        public string DeviceId { get; set; }

        public string DeviceMsgReceived { get; set; }
    }
}