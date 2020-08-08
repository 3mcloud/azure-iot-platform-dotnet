// <copyright file="JobsController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mmm.Iot.Common.Services.Filters;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.IoTHubManager.Services;
using Mmm.Iot.IoTHubManager.Services.Models;
using Mmm.Iot.IoTHubManager.WebService.Models;

namespace Mmm.Iot.IoTHubManager.WebService.Controllers
{
    [Route("v1/[controller]")]
    [TypeFilter(typeof(ExceptionsFilterAttribute))]
    public class JobsController : Controller
    {
        private readonly IJobs jobs;

        public JobsController(IJobs jobs)
        {
            this.jobs = jobs;
        }

        [HttpGet]
        [Authorize("ReadAll")]
        public async Task<IEnumerable<JobApiModel>> GetAsync(
            [FromQuery] JobType? jobType,
            [FromQuery] JobStatus? jobStatus,
            [FromQuery] int? pageSize,
            [FromQuery] string from,
            [FromQuery] string to)
        {
            var result = await this.jobs.GetJobsAsync(jobType, jobStatus, pageSize, from, to);
            return result.Select(r => new JobApiModel(r));
        }

        [HttpGet("{jobId}")]
        [Authorize("ReadAll")]
        public async Task<JobApiModel> GetJobAsync(
            string jobId,
            [FromQuery]bool? includeDeviceDetails,
            [FromQuery]DeviceJobStatus? deviceJobStatus)
        {
            var result = await this.jobs.GetJobsAsync(jobId, includeDeviceDetails, deviceJobStatus);
            return new JobApiModel(result);
        }

        [HttpPost]
        [Authorize("CreateJobs")]
        public async Task<JobApiModel> ScheduleAsync([FromBody] JobApiModel parameter)
        {
            if (parameter.UpdateTwin != null)
            {
                var result = await this.jobs.ScheduleTwinUpdateAsync(parameter.JobId, parameter.QueryCondition, parameter.UpdateTwin.ToServiceModel(), parameter.StartTimeUtc ?? DateTime.UtcNow, parameter.MaxExecutionTimeInSeconds ?? 0);
                return new JobApiModel(result);
            }

            if (parameter.MethodParameter != null)
            {
                var result = await this.jobs.ScheduleDeviceMethodAsync(parameter.JobId, parameter.QueryCondition, parameter.MethodParameter.ToServiceModel(), parameter.StartTimeUtc ?? DateTime.UtcNow, parameter.MaxExecutionTimeInSeconds ?? 0);
                return new JobApiModel(result);
            }

            throw new NotSupportedException();
        }
    }
}