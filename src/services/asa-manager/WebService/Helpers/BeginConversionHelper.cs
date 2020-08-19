// <copyright file="BeginConversionHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mmm.Iot.AsaManager.Services.External.IotHubManager;
using Mmm.Iot.AsaManager.Services.Models.IotHub;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.Common.Services.Wrappers;

namespace Mmm.Iot.AsaManager.WebService.Helpers
{
    public class BeginConversionHelper : IBeginConversionHelper
    {
        private const int JobWatchRetryCount = 12;
        private const int JobWatchDelay = 10000;

        private readonly ILogger logger;
        private readonly IKeyGenerator keyGenerator;
        private readonly IIotHubManagerClient iotHubManagerClient;

        public BeginConversionHelper(
            IIotHubManagerClient iotHubManagerClient,
            ILogger<BeginConversionHelper> logger,
            IKeyGenerator keyGenerator)
        {
            this.iotHubManagerClient = iotHubManagerClient;
            this.logger = logger;
            this.keyGenerator = keyGenerator;
        }

        public string BeginConversion(ConvertAsync convertAsync, string tenantId)
        {
            string operationId = this.keyGenerator.Generate();

            convertAsync(tenantId, operationId)
                .ContinueWith(
                    t => this.LogBackgroundException(t.Exception, operationId),
                    TaskContinuationOptions.OnlyOnFaulted);

            return operationId;
        }

        public string WatchIotHubJobAndBeginConversion(string jobId, ConvertAsync convertAsync, string tenantId)
        {
            string operationId = this.keyGenerator.Generate();

            this.IotHubJobDelayedConversion(
                jobId,
                convertAsync,
                tenantId,
                operationId)
                .ContinueWith(
                    t => this.LogBackgroundException(t.Exception, operationId),
                    TaskContinuationOptions.OnlyOnFaulted);

            return operationId;
        }

        private void LogBackgroundException(Exception exception, string operationId)
        {
            this.logger.LogError(exception, "An exception occurred during the background conversion. OperationId {operationId}", operationId);
        }

        private async Task IotHubJobDelayedConversion(
            string jobId,
            ConvertAsync convertAsync,
            string tenantId,
            string operationId)
        {
            int getJobErrorCount = 0;
            int watchRetryCount = 0;
            JobModel job = new JobModel
            {
                JobId = jobId,
                Status = JobStatus.Unknown,
            };

            do
            {
                try
                {
                    job = await this.iotHubManagerClient.GetJobAsync(jobId, tenantId);
                    Console.WriteLine(job);

                    if (watchRetryCount == 0
                        && job.StartTimeUtc != null
                        && (job.StartTimeUtc - DateTime.Now).Value.TotalMilliseconds > (JobWatchRetryCount * JobWatchDelay))
                    {
                        // on the first check, see if the start time is within the maximum delay
                        throw new Exception("Jobs that are not set to start within two minutes of this request are not supported. Conversion will not be triggered");
                    }

                    if (job.EndTimeUtc != null && job.EndTimeUtc > DateTime.Now)
                    {
                        await convertAsync(tenantId, operationId)
                            .ContinueWith(
                                t => this.LogBackgroundException(t.Exception, operationId),
                                TaskContinuationOptions.OnlyOnFaulted);
                        return;
                    }
                }
                catch (ExternalDependencyException)
                {
                    getJobErrorCount += 1;
                }

                switch (job.Status)
                {
                    case JobStatus.Completed:
                        await convertAsync(tenantId, operationId)
                            .ContinueWith(
                                t => this.LogBackgroundException(t.Exception, operationId),
                                TaskContinuationOptions.OnlyOnFaulted);
                        return;
                    case JobStatus.Failed:
                        throw new Exception("The given job failed. Conversion will not be triggered.");
                    default:
                        watchRetryCount += 1;
                        Thread.Sleep(JobWatchDelay);
                        break;
                }
            }
            while (getJobErrorCount < 4 && watchRetryCount < JobWatchRetryCount);

            if (getJobErrorCount > 3)
            {
                throw new Exception("The given job ocould not be found. Conversion will not be triggered.");
            }
            else
            {
                throw new Exception($"The job did not complete within the maximum watch time ({(JobWatchRetryCount * JobWatchDelay) / 1000} seconds). Conversion will not be triggered.");
            }
        }
    }
}