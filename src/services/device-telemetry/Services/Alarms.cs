// <copyright file="Alarms.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.AppConfiguration;
using Mmm.Iot.Common.Services.External.CosmosDb;
using Mmm.Iot.Common.Services.Helpers;
using Mmm.Iot.DeviceTelemetry.Services.Models;

namespace Mmm.Iot.DeviceTelemetry.Services
{
    public class Alarms : IAlarms
    {
        private const string MessageReceivedKey = "deviceMsgReceived";
        private const string RuleIdKey = "ruleId";
        private const string DeviceIdKey = "deviceId";
        private const string StatusKey = "status";
        private const string AlarmSchemaKey = "alarm";
        private const string AlarmStatusOpen = "open";
        private const string AlarmStatusAcknowledged = "acknowledged";
        private const string TenantInfoKey = "tenant";
        private const string AlarmsCollectionKey = "alarms-collection";
        private const int DocumentQueryLimit = 1000;
        private readonly string databaseName;
        private readonly int maxDeleteRetryCount;
        private readonly ILogger logger;
        private readonly IStorageClient storageClient;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IAppConfigurationClient appConfigurationClient;

        public Alarms(
            AppConfig config,
            IStorageClient storageClient,
            ILogger<Alarms> logger,
            IHttpContextAccessor contextAccessor,
            IAppConfigurationClient appConfigurationClient)
        {
            this.storageClient = storageClient;
            this.databaseName = config.DeviceTelemetryService.Alarms.Database;
            this.logger = logger;
            this.maxDeleteRetryCount = config.DeviceTelemetryService.Alarms.MaxDeleteRetries;
            this.httpContextAccessor = contextAccessor;
            this.appConfigurationClient = appConfigurationClient;
        }

        private string CollectionId
        {
            get
            {
                return this.appConfigurationClient.GetValue(
                    $"{TenantInfoKey}:{this.httpContextAccessor.HttpContext.Request.GetTenant()}:{AlarmsCollectionKey}");
            }
        }

        public async Task<Alarm> GetAsync(string id)
        {
            Document doc = await this.GetDocumentByIdAsync(id);
            return new Alarm(doc);
        }

        public async Task<List<Alarm>> ListAsync(
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] devices)
        {
            var sql = QueryBuilder.GetDocumentsSql(
                AlarmSchemaKey,
                null,
                null,
                from,
                MessageReceivedKey,
                to,
                MessageReceivedKey,
                order,
                MessageReceivedKey,
                skip,
                limit,
                devices,
                DeviceIdKey);

            this.logger.LogDebug("Created alarm query {sql}", sql);

            FeedOptions queryOptions = new FeedOptions
            {
                EnableCrossPartitionQuery = false,
                EnableScanInQuery = true,
            };

            try
            {
                List<Document> docs = await this.storageClient.QueryDocumentsAsync(
                    this.databaseName,
                    this.CollectionId,
                    queryOptions,
                    sql,
                    skip,
                    limit);

                return docs == null ?
                    new List<Alarm>() :
                    docs
                        .Select(doc => new Alarm(doc))
                        .ToList();
            }
            catch (ResourceNotFoundException e)
            {
                throw new ResourceNotFoundException($"No alarms exist in CosmosDb. The alarms collection {this.CollectionId} does not exist.", e);
            }
        }

        public async Task<List<Alarm>> ListByRuleAsync(
            string id,
            DateTimeOffset? from,
            DateTimeOffset? to,
            string order,
            int skip,
            int limit,
            string[] devices)
        {
            var sql = QueryBuilder.GetDocumentsSql(
                AlarmSchemaKey,
                id,
                RuleIdKey,
                from,
                MessageReceivedKey,
                to,
                MessageReceivedKey,
                order,
                MessageReceivedKey,
                skip,
                limit,
                devices,
                DeviceIdKey);

            this.logger.LogDebug("Created alarm by rule query {sql}", sql);

            FeedOptions queryOptions = new FeedOptions
            {
                EnableCrossPartitionQuery = false,
                EnableScanInQuery = true,
            };

            try
            {
                List<Document> docs = await this.storageClient.QueryDocumentsAsync(
                    this.databaseName,
                    this.CollectionId,
                    queryOptions,
                    sql,
                    skip,
                    limit);

                return docs == null ?
                    new List<Alarm>() :
                    docs
                        .Select(doc => new Alarm(doc))
                        .ToList();
            }
            catch (ResourceNotFoundException e)
            {
                throw new ResourceNotFoundException($"No alarms exist in CosmosDb. The alarms collection {this.CollectionId} does not exist.", e);
            }
        }

        public async Task<int> GetCountByRuleAsync(
            string id,
            DateTimeOffset? from,
            DateTimeOffset? to,
            string[] devices)
        {
            // build sql query to get open/acknowledged alarm count for rule
            string[] statusList = { AlarmStatusOpen, AlarmStatusAcknowledged };
            var sql = QueryBuilder.GetCountSql(
                AlarmSchemaKey,
                id,
                RuleIdKey,
                from,
                MessageReceivedKey,
                to,
                MessageReceivedKey,
                devices,
                DeviceIdKey,
                statusList,
                StatusKey);

            FeedOptions queryOptions = new FeedOptions
            {
                EnableCrossPartitionQuery = false,
                EnableScanInQuery = true,
            };

            try
            {
                return await this.storageClient.QueryCountAsync(
                    this.databaseName,
                    this.CollectionId,
                    queryOptions,
                    sql);
            }
            catch (ResourceNotFoundException e)
            {
                throw new ResourceNotFoundException($"No alarms exist in CosmosDb. The alarms collection {this.CollectionId} does not exist.", e);
            }
        }

        public async Task<Alarm> UpdateAsync(string id, string status)
        {
            InputValidator.Validate(id);
            InputValidator.Validate(status);

            Document document = await this.GetDocumentByIdAsync(id);
            document.SetPropertyValue(StatusKey, status);

            document = await this.storageClient.UpsertDocumentAsync(
                this.databaseName,
                this.CollectionId,
                document);

            return new Alarm(document);
        }

        public async Task Delete(List<string> ids)
        {
            foreach (var id in ids)
            {
                InputValidator.Validate(id);
            }

            Task[] taskList = new Task[ids.Count];
            for (int i = 0; i < ids.Count; i++)
            {
                taskList[i] = this.DeleteAsync(ids[i]);
            }

            try
            {
                await Task.WhenAll(taskList);
            }
            catch (AggregateException aggregateException)
            {
                Exception inner = aggregateException.InnerExceptions[0];
                this.logger.LogError(inner, "Failed to delete alarm");
                throw inner;
            }
        }

        /**
         * Delete an individual alarm by id. If the delete fails for a DocumentClientException
         * other than not found, retry up to this.maxRetryCount
         */
        public async Task DeleteAsync(string id)
        {
            InputValidator.Validate(id);

            int retryCount = 0;
            while (retryCount < this.maxDeleteRetryCount)
            {
                try
                {
                    await this.storageClient.DeleteDocumentAsync(
                        this.databaseName,
                        this.CollectionId,
                        id);
                    return;
                }
                catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
                {
                    return;
                }
                catch (Exception e)
                {
                    // only delay if there is a suggested retry (i.e. if the request is throttled)
                    TimeSpan retryTimeSpan = TimeSpan.Zero;
                    if (e.GetType() == typeof(DocumentClientException))
                    {
                        retryTimeSpan = ((DocumentClientException)e).RetryAfter;
                    }

                    retryCount++;

                    if (retryCount >= this.maxDeleteRetryCount)
                    {
                        this.logger.LogError(e, "Failed to delete alarm {id}", id);
                        throw new ExternalDependencyException(e.Message);
                    }

                    this.logger.LogWarning(e, "Exception on delete alarm {id}", id);
                    Thread.Sleep(retryTimeSpan);
                }
            }
        }

        private async Task<Document> GetDocumentByIdAsync(string id)
        {
            InputValidator.Validate(id);

            var query = new SqlQuerySpec(
                "SELECT * FROM c WHERE c.id=@id",
                new SqlParameterCollection(new SqlParameter[]
                {
                    new SqlParameter { Name = "@id", Value = id },
                }));

            // Retrieve the document using the DocumentClient.
            List<Document> documentList = await this.storageClient.QueryDocumentsAsync(
                this.databaseName,
                this.CollectionId,
                null,
                query,
                0,
                DocumentQueryLimit);

            if (documentList.Count > 0)
            {
                return documentList[0];
            }

            return null;
        }
    }
}