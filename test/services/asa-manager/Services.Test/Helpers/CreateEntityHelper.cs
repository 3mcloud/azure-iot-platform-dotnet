// <copyright file="CreateEntityHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Mmm.Iot.AsaManager.Services.Models.DeviceGroups;
using Mmm.Iot.AsaManager.Services.Models.Rules;
using Mmm.Iot.Common.Services.External.StorageAdapter;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.Common.TestHelpers;
using Newtonsoft.Json;

namespace Mmm.Iot.AsaManager.Services.Test.Helpers
{
    public class CreateEntityHelper
    {
        private readonly Random rand;

        public CreateEntityHelper(Random rand)
        {
            this.rand = rand;
        }

        public ValueApiModel CreateRule()
        {
            RuleDataModel data = new RuleDataModel
            {
                Conditions = new List<ConditionModel>(),
                Actions = new List<IActionModel>(),
                Enabled = true,
                Deleted = false,
                TimePeriod = 60000,  // a value from timePeriodMap, a field of the RuleReferenceDatModel
                Name = this.rand.NextString(),
                Description = this.rand.NextString(),
                GroupId = this.rand.NextString(),
                Severity = this.rand.NextString(),
                Calculation = this.rand.NextString(),
            };

            return new ValueApiModel
            {
                Key = this.rand.NextString(),
                ETag = this.rand.NextString(),
                Data = JsonConvert.SerializeObject(data),
            };
        }

        public ValueApiModel CreateDeviceGroup()
        {
            DeviceGroupDataModel data = new DeviceGroupDataModel
            {
                Conditions = new List<DeviceGroupConditionModel>(),
                DisplayName = this.rand.NextString(),
            };

            return new ValueApiModel
            {
                Key = this.rand.NextString(),
                ETag = this.rand.NextString(),
                Data = JsonConvert.SerializeObject(data),
            };
        }

        public DeviceGroupConditionModel CreateDeviceGroupConditionModel()
        {
            return new DeviceGroupConditionModel
            {
                Key = this.rand.NextString(),
                Operator = DeviceGroupConditionOperatorType.EQ,
                Value = this.rand.NextString(),
            };
        }

        public DeviceModel CreateDevice()
        {
            return new DeviceModel
            {
                Id = this.rand.NextString(),
            };
        }
    }
}