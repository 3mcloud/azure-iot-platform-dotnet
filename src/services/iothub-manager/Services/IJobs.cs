// <copyright file="IJobs.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mmm.Iot.IoTHubManager.Services.Models;
using DeviceJobStatus = Mmm.Iot.IoTHubManager.Services.Models.DeviceJobStatus;
using JobStatus = Mmm.Iot.Common.Services.Models.JobStatus;
using JobType = Mmm.Iot.IoTHubManager.Services.Models.JobType;

namespace Mmm.Iot.IoTHubManager.Services
{
    public interface IJobs
    {
        Task<IEnumerable<JobServiceModel>> GetJobsAsync(
            JobType? jobType,
            JobStatus? jobStatus,
            int? pageSize,
            string queryFrom,
            string queryTo);

        Task<JobServiceModel> GetJobsAsync(
            string jobId,
            bool? includeDeviceDetails,
            DeviceJobStatus? deviceJobStatus);

        Task<JobServiceModel> ScheduleDeviceMethodAsync(
            string jobId,
            string queryCondition,
            MethodParameterServiceModel parameter,
            DateTimeOffset startTimeUtc,
            long maxExecutionTimeInSeconds);

        Task<JobServiceModel> ScheduleTwinUpdateAsync(
            string jobId,
            string queryCondition,
            TwinServiceModel twin,
            DateTimeOffset startTimeUtc,
            long maxExecutionTimeInSeconds);
    }
}