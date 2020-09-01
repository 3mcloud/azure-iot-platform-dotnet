// <copyright file="AlertingContainer.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.AppConfiguration;
using Mmm.Iot.Common.Services.External.CosmosDb;
using Mmm.Iot.Common.Services.External.TableStorage;
using Mmm.Iot.TenantManager.Services.Helpers;
using Mmm.Iot.TenantManager.Services.Models;

namespace Mmm.Iot.TenantManager.Services
{
    public class AlertingContainer : IAlertingContainer
    {
        private const string AlarmsCollectionAppConfigKeyFormat = "tenant:{0}:alarms-collection";
        private const string AlarmsCollectionNameFormat = "alarms-{0}";
        private const string AlarmsCollectionPartitionKey = "deviceId";
        private const string AlarmsDatabaseName = "iot";
        private const string SaNameFormat = "sa-{0}";

        private readonly ITenantContainer tenantContainer;
        private readonly IStreamAnalyticsHelper streamAnalyticsHelper;
        private readonly IRunbookHelper runbookHelper;
        private readonly IStorageClient cosmosDb;
        private readonly IAppConfigurationClient appConfig;
        private readonly ITableStorageClient tableStorageClient;

        public AlertingContainer(
            ITableStorageClient tableStorageClient,
            ITenantContainer tenantContainer,
            IStreamAnalyticsHelper streamAnalyticsHelper,
            IRunbookHelper runbookHelper,
            IStorageClient cosmosDb,
            IAppConfigurationClient appConfig)
        {
            this.tenantContainer = tenantContainer;
            this.streamAnalyticsHelper = streamAnalyticsHelper;
            this.runbookHelper = runbookHelper;
            this.cosmosDb = cosmosDb;
            this.appConfig = appConfig;
            this.tableStorageClient = tableStorageClient;
        }

        public bool SaJobExists(StreamAnalyticsJobModel saJobModel)
        {
            return !string.IsNullOrEmpty(saJobModel.StreamAnalyticsJobName) && !string.IsNullOrEmpty(saJobModel.JobState);
        }

        public async Task<StreamAnalyticsJobModel> AddAlertingAsync(string tenantId)
        {
            string alarmsCollectionAppConfigKey = string.Format(AlarmsCollectionAppConfigKeyFormat, tenantId);
            string alarmsCollectionId;
            try
            {
                alarmsCollectionId = this.appConfig.GetValue(alarmsCollectionAppConfigKey);
            }
            catch (Exception)
            {
                alarmsCollectionId = string.Format(AlarmsCollectionNameFormat, tenantId);
            }

            try
            {
                await this.cosmosDb.CreateCollectionIfNotExistsAsync(
                    AlarmsDatabaseName,
                    alarmsCollectionId,
                    AlarmsCollectionPartitionKey);
                await this.appConfig.SetValueAsync(alarmsCollectionAppConfigKey, alarmsCollectionId);
            }
            catch (Exception e)
            {
                throw new Exception("Unable to ensure the necessary AppConfig configuration and CosmosDb collection exist before creating the stream analytics job.", e);
            }

            StreamAnalyticsJobModel saJobModel = await this.GetAlertingAsync(tenantId);
            if (this.SaJobExists(saJobModel))
            {
                throw new Exception("The given tenant already has a deployed stream analytics job.");
            }

            TenantModel tenant = await this.GetTenantFromContainerAsync(tenantId);
            string saJobName = string.Format(SaNameFormat, tenantId.Substring(0, 8));

            await this.tableStorageClient.InsertAsync(
                "tenantOperations",
                new TenantOperationModel(tenantId, TenantOperation.SaJobCreation, saJobName));

            return new StreamAnalyticsJobModel
            {
                TenantId = tenant.TenantId,
                StreamAnalyticsJobName = saJobName,
                IsActive = false,
                JobState = "Creating",
            };
        }

        public async Task<StreamAnalyticsJobModel> RemoveAlertingAsync(string tenantId)
        {
            TenantModel tenant = await this.GetTenantFromContainerAsync(tenantId);
            await this.tableStorageClient.InsertAsync(
                "tenantOperations",
                new TenantOperationModel(tenantId, TenantOperation.SaJobDeletion, tenant.SAJobName));
            tenant.SAJobName = null;
            await this.tableStorageClient.InsertOrMergeAsync("tenant", tenant);

            return new StreamAnalyticsJobModel
            {
                TenantId = tenant.TenantId,
                StreamAnalyticsJobName = tenant.SAJobName,
                IsActive = false,
                JobState = "Deleting",
            };
        }

        public async Task<StreamAnalyticsJobModel> GetAlertingAsync(string tenantId)
        {
            TenantModel tenant = await this.GetTenantFromContainerAsync(tenantId);
            try
            {
                var job = await this.streamAnalyticsHelper.GetJobAsync(tenant.SAJobName);
                return new StreamAnalyticsJobModel
                {
                    TenantId = tenant.TenantId,
                    JobState = job.JobState,
                    IsActive = this.streamAnalyticsHelper.JobIsActive(job),
                    StreamAnalyticsJobName = job.Name,
                };
            }
            catch (ResourceNotFoundException)
            {
                // Return a model with null information regarding the stream analytics job if it does not exist
                return new StreamAnalyticsJobModel
                {
                    TenantId = tenant.TenantId,
                    IsActive = false,
                };
            }
            catch (Exception e)
            {
                throw new Exception("An Unknown exception occurred while attempting to get the tenant's stream analytics job.", e);
            }
        }

        public async Task<StreamAnalyticsJobModel> StartAlertingAsync(string tenantId)
        {
            StreamAnalyticsJobModel saJobModel = await this.GetAlertingAsync(tenantId);
            if (!this.SaJobExists(saJobModel))
            {
                throw new Exception("There is no StreamAnalyticsJob is available to start for this tenant.");
            }

            await this.streamAnalyticsHelper.StartAsync(saJobModel.StreamAnalyticsJobName);
            return saJobModel;
        }

        public async Task<StreamAnalyticsJobModel> StopAlertingAsync(string tenantId)
        {
            StreamAnalyticsJobModel saJobModel = await this.GetAlertingAsync(tenantId);
            if (!this.SaJobExists(saJobModel))
            {
                throw new Exception("There is no StreamAnalyticsJob is available to stop for this tenant.");
            }

            await this.streamAnalyticsHelper.StopAsync(saJobModel.StreamAnalyticsJobName);
            return saJobModel;
        }

        private async Task<TenantModel> GetTenantFromContainerAsync(string tenantId)
        {
            TenantModel tenant = await this.tenantContainer.GetTenantAsync(tenantId);
            if (tenant == null)
            {
                throw new Exception("The given tenant does not exist.");
            }

            bool tenantReady = await this.tenantContainer.TenantIsReadyAsync(tenantId);
            if (!tenantReady)
            {
                throw new Exception("The tenant is not fully deployed yet. Please wait for the tenant to fully deploy before performing alerting operations");
            }

            return tenant;
        }
    }
}