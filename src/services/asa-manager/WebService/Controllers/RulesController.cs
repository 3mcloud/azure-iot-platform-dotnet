// <copyright file="RulesController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mmm.Iot.AsaManager.Services;
using Mmm.Iot.AsaManager.WebService.Helpers;
using Mmm.Iot.Common.Services;
using Mmm.Iot.Common.Services.External.AsaManager;
using Mmm.Iot.Common.Services.Filters;
using Mmm.Iot.Common.Services.Wrappers;

namespace Mmm.Iot.AsaManager.WebService.Controllers
{
    [Route("v1/[controller]")]
    [TypeFilter(typeof(ExceptionsFilterAttribute))]
    public class RulesController : Controller
    {
        private readonly IConverter ruleConverter;
        private readonly IBeginConversionHelper beginConversionHelper;

        public RulesController(
            RulesConverter ruleConverter,
            IBeginConversionHelper beginConversionHelper)
        {
            this.ruleConverter = ruleConverter;
            this.beginConversionHelper = beginConversionHelper;
        }

        [HttpPost("")]
        public BeginConversionApiModel BeginRuleConversion()
        {
            string tenantId = this.GetTenantId();
            string operationId = this.beginConversionHelper.BeginConversion(
                this.ruleConverter.ConvertAsync,
                tenantId);

            // Return the operationId of the Rule conversion synchronous process
            return new BeginConversionApiModel
            {
                TenantId = tenantId,
                OperationId = operationId,
            };
        }
    }
}