// <copyright file="EmailFormatModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Mmm.Iot.Functions.RuleActions.Models
{
    public class EmailFormatModel
    {
        public List<string> Recipients { get; set; }

        public string Subject { get; set; }

        public string DeviceId { get; set; }

        public string Description { get; set; }

        public string Severity { get; set; }

        public string Time { get; set; }

        public string Notes { get; set; }
    }
}