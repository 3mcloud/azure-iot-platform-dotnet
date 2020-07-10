// <copyright file="StorageClient.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.Models;
using Index = Microsoft.Azure.Documents.Index;

namespace Mmm.Iot.Common.Services.External.CosmosDb
{
    public class StorageClient : IStorageClient
    {
        private const string ConnectionStringValueRegex = @"^AccountEndpoint=(?<endpoint>.*);AccountKey=(?<key>.*);$";
        private readonly ILogger logger;
        private Uri storageUri;
        private string storagePrimaryKey;
        private int storageThroughput;
        private IDocumentClient client;

        public StorageClient(AppConfig config, ILogger<StorageClient> logger)
        {
            this.SetValuesFromConfig(config);
            this.logger = logger;
            this.client = this.GetDocumentClient();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageClient"/> class.
        /// Constructor used in testing.
        /// </summary>
        /// <param name="config"> app config to be used.</param>
        /// <param name="logger">logger to be used. </param>
        /// <param name="documentClient"> document client to be used. </param>
        public StorageClient(AppConfig config, ILogger<StorageClient> logger, IDocumentClient documentClient)
        {
            this.SetValuesFromConfig(config);
            this.logger = logger;
            this.client = documentClient;
        }

        [ExcludeFromCodeCoverage]
        public async Task DeleteDatabaseAsync(string databaseName)
        {
            try
            {
                await this.client.DeleteDatabaseAsync($"/dbs/{databaseName}");
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Unable to delete database {databaseName}", databaseName);
                throw;
            }
        }

        [ExcludeFromCodeCoverage]
        public async Task CreateDatabaseIfNotExistsAsync(
            string databaseName)
        {
            Database database = new Database
            {
                Id = databaseName,
            };

            try
            {
                await this.client.CreateDatabaseIfNotExistsAsync(database);
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Unable to create database {databaseName}", databaseName);
                throw;
            }
        }

        [ExcludeFromCodeCoverage]
        public async Task DeleteCollectionAsync(
            string databaseName,
            string id)
        {
            string collectionLink = $"/dbs/{databaseName}/colls/{id}";
            try
            {
                await this.client.DeleteDocumentCollectionAsync(collectionLink);
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Unable to delete collection {id}.", databaseName);
                throw;
            }
        }

        public async Task<ResourceResponse<DocumentCollection>> CreateCollectionIfNotExistsAsync(
            string databaseName,
            string id,
            string partitionKeyName = null)
        {
            RangeIndex index = Index.Range(DataType.String, -1);
            DocumentCollection collectionInfo = new DocumentCollection
            {
                IndexingPolicy = new IndexingPolicy(new Index[] { index }),
                Id = id,
            };

            if (!string.IsNullOrEmpty(partitionKeyName))
            {
                collectionInfo.PartitionKey = new PartitionKeyDefinition
                {
                    Paths = new Collection<string> { $"/{partitionKeyName}" },
                };
            }

            // Azure Cosmos DB collections can be reserved with
            // throughput specified in request units/second.
            RequestOptions requestOptions = new RequestOptions
            {
                OfferThroughput = this.storageThroughput,
                ConsistencyLevel = ConsistencyLevel.Strong,
            };
            string dbUrl = "/dbs/" + databaseName;

            try
            {
                await this.CreateDatabaseIfNotExistsAsync(databaseName);
            }
            catch (Exception e)
            {
                throw new Exception($"While attempting to create collection {id}, an error occured while attepmting to create its database {databaseName}.", e);
            }

            ResourceResponse<DocumentCollection> response;
            try
            {
                response = await this.client.CreateDocumentCollectionIfNotExistsAsync(
                    dbUrl,
                    collectionInfo,
                    requestOptions);
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Error creating collection with ID {id}, database URL {databaseUrl}, and collection info {collectionInfo}", id, dbUrl, collectionInfo);
                throw;
            }

            return response;
        }

        public async Task<Document> ReadDocumentAsync(
            string databaseName,
            string colId,
            string docId)
        {
            string docUrl = string.Format(
                "/dbs/{0}/colls/{1}/docs/{2}",
                databaseName,
                colId,
                docId);

            try
            {
                return await this.client.ReadDocumentAsync(docUrl);
            }
            catch (DocumentClientException dce)
            {
                throw this.ConvertDocumentClientException(dce);
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Error reading document in collection with collection ID {collectionId}", colId);
                throw;
            }
        }

        public async Task<Document> CreateDocumentAsync(
            string databaseName,
            string colId,
            object document)
        {
            string colUrl = string.Format("/dbs/{0}/colls/{1}", databaseName, colId);

            await this.CreateCollectionIfNotExistsAsync(databaseName, colId);
            try
            {
                return await this.client.CreateDocumentAsync(colUrl, document, new RequestOptions(), false);
            }
            catch (DocumentClientException dce)
            {
                throw this.ConvertDocumentClientException(dce);
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Error upserting document into collection with collection ID {collectionId}", colId);
                throw;
            }
        }

        public async Task<Document> DeleteDocumentAsync(
            string databaseName,
            string colId,
            string docId)
        {
            string docUrl = string.Format(
                "/dbs/{0}/colls/{1}/docs/{2}",
                databaseName,
                colId,
                docId);

            await this.CreateCollectionIfNotExistsAsync(databaseName, colId);
            try
            {
                return await this.client.DeleteDocumentAsync(
                    docUrl,
                    new RequestOptions());
            }
            catch (DocumentClientException dce)
            {
                throw this.ConvertDocumentClientException(dce);
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Error deleting document in collection with collection ID {collectionId}", colId);
                throw;
            }
        }

        public IDocumentClient GetDocumentClient()
        {
            if (this.client == null)
            {
                string errorMessage = $"Could not connect to storage at URI {this.storageUri}";
                try
                {
                    this.client = new DocumentClient(
                        this.storageUri,
                        this.storagePrimaryKey,
                        ConnectionPolicy.Default,
                        ConsistencyLevel.Session);
                }
                catch (Exception e)
                {
                    this.logger.LogError(e, errorMessage, "check connection string");
                    throw new InvalidConfigurationException(
                        "Could not connect to DocumentClient, " +
                        "check connection string");
                }

                if (this.client == null)
                {
                    this.logger.LogError(new Exception(errorMessage), errorMessage);
                    throw new InvalidConfigurationException("Could not connect to DocumentClient");
                }
            }

            return this.client;
        }

        public async Task<StatusResultServiceModel> StatusAsync()
        {
            var result = new StatusResultServiceModel(false, "Storage check failed");

            try
            {
                DatabaseAccount response = null;
                if (this.client != null)
                {
                    // make generic call to see if storage client can be reached
                    response = await this.client.GetDatabaseAccountAsync();
                }

                if (response != null)
                {
                    result.IsHealthy = true;
                    result.Message = "Alive and Well!";
                }
            }
            catch (Exception e)
            {
                this.logger.LogInformation(e, result.Message);
            }

            return result;
        }

        [ExcludeFromCodeCoverage]
        public async Task<List<Document>> QueryAllDocumentsAsync(
            string databaseName,
            string colId)
        {
            string collectionLink = string.Format(
                "/dbs/{0}/colls/{1}",
                databaseName,
                colId);

            try
            {
                return await Task.FromResult(this.client.CreateDocumentQuery(collectionLink).ToList<Document>());
            }
            catch (AggregateException ae)
            {
                if (ae.InnerException is DocumentClientException)
                {
                    throw this.ConvertDocumentClientException(ae.InnerException as DocumentClientException);
                }
                else
                {
                    throw;
                }
            }
            catch (DocumentClientException dce)
            {
                throw this.ConvertDocumentClientException(dce);
            }
        }

        public async Task<List<Document>> QueryDocumentsAsync(
            string databaseName,
            string colId,
            FeedOptions queryOptions,
            SqlQuerySpec querySpec,
            int skip,
            int limit)
        {
            if (queryOptions == null)
            {
                queryOptions = new FeedOptions
                {
                    EnableCrossPartitionQuery = true,
                    EnableScanInQuery = true,
                };
            }

            string collectionLink = string.Format(
                "/dbs/{0}/colls/{1}",
                databaseName,
                colId);

            try
            {
                var result = await Task.FromResult(this.client.CreateDocumentQuery<Document>(
                        collectionLink,
                        querySpec,
                        queryOptions));

                var queryResults = result == null ?
                    new List<Document>() :
                    result
                        .AsEnumerable()
                        .Skip(skip)
                        .Take(limit)
                        .ToList();

                this.logger.LogInformation("Query results count: {count}", queryResults.Count);

                return queryResults;
            }
            catch (AggregateException ae)
            {
                if (ae.InnerException is DocumentClientException)
                {
                    throw this.ConvertDocumentClientException(ae.InnerException as DocumentClientException);
                }
                else
                {
                    throw;
                }
            }
            catch (DocumentClientException dce)
            {
                throw this.ConvertDocumentClientException(dce);
            }
        }

        [ExcludeFromCodeCoverage]
        public async Task<int> QueryCountAsync(
            string databaseName,
            string colId,
            FeedOptions queryOptions,
            SqlQuerySpec querySpec)
        {
            if (queryOptions == null)
            {
                queryOptions = new FeedOptions
                {
                    EnableCrossPartitionQuery = false,
                    EnableScanInQuery = true,
                };
            }

            string collectionLink = string.Format(
                "/dbs/{0}/colls/{1}",
                databaseName,
                colId);

            await this.CreateCollectionIfNotExistsAsync(databaseName, colId);

            try
            {
                var result = this.client.CreateDocumentQuery(
                    collectionLink,
                    querySpec,
                    queryOptions);

                var resultList = result == null ? new Document[0] : result.ToArray();

                return resultList.Length > 0 ? (int)resultList[0] : 0;
            }
            catch (AggregateException ae)
            {
                if (ae.InnerException is DocumentClientException)
                {
                    throw this.ConvertDocumentClientException(ae.InnerException as DocumentClientException);
                }
                else
                {
                    throw;
                }
            }
            catch (DocumentClientException dce)
            {
                throw this.ConvertDocumentClientException(dce);
            }
        }

        public async Task<Document> UpsertDocumentAsync(
            string databaseName,
            string colId,
            object document)
        {
            string colUrl = string.Format("/dbs/{0}/colls/{1}", databaseName, colId);

            await this.CreateCollectionIfNotExistsAsync(databaseName, colId);
            try
            {
                return await this.client.UpsertDocumentAsync(
                    colUrl,
                    document,
                    new RequestOptions(),
                    false);
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Error upserting document into collection with collection ID {collectionId}", colId);
                throw;
            }
        }

        [ExcludeFromCodeCoverage]
        private void SetValuesFromConfig(AppConfig config)
        {
            if (string.IsNullOrEmpty(config.Global.CosmosDb.DocumentDbConnectionString))
            {
                throw new ArgumentNullException("The CosmosDbConnectionString in the IStorageClientConfig was null or empty. The StorageClient cannot be created with an empty connection string.");
            }

            try
            {
                Match match = Regex.Match(config.Global.CosmosDb.DocumentDbConnectionString, ConnectionStringValueRegex);

                // Get the storage uri from the regular expression match
                Uri.TryCreate(match.Groups["endpoint"].Value, UriKind.RelativeOrAbsolute, out Uri storageUriEndpoint);
                this.storageUri = storageUriEndpoint;
                if (string.IsNullOrEmpty(this.storageUri.ToString()))
                {
                    throw new Exception("The StorageUri dissected from the connection string was null. The connection string may be null or not formatted correctly.");
                }

                // Get the PrimaryKey from the connection string
                this.storagePrimaryKey = match.Groups["key"]?.Value;
                if (string.IsNullOrEmpty(this.storagePrimaryKey))
                {
                    throw new Exception("The StoragePrimaryKey dissected from the connection string was null. The connection string may be null or not formatted correctly.");
                }
            }
            catch (Exception e)
            {
                throw new Exception("Unable to create required StorageClient fields using the connection string from the IStorageClientConfig instance.", e);
            }

            // handling exceptions is not necessary here - the value can be left null if not configured.
            this.storageThroughput = config.Global.CosmosDb.Rus;
        }

        private Exception ConvertDocumentClientException(DocumentClientException e)
        {
            switch (e.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    return new ResourceNotFoundException(e.Message, e);
                case HttpStatusCode.Conflict:
                    return new ConflictingResourceException(e.Message, e);
                default:
                    return e;
            }
        }
    }
}