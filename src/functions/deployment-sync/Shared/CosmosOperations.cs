// <copyright file="CosmosOperations.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Mmm.Iot.Functions.DeploymentSync.Shared.Exceptions;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class CosmosOperations
    {
        private const string InvalidCharacterRegex = @"[^A-Za-z0-9:;.,_\-@]";
        private static CosmosOperations instance = null;
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly DocumentClient client = null;
        private RequestOptions requestOptions;

        private CosmosOperations(DocumentClient client)
        {
            this.client = client;
        }

        public static async Task<CosmosOperations> GetClientAsync()
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                return instance ?? (instance = CreateInstance());
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public static SqlQuerySpec GetDocumentsByCollectionId(
            string key,
            string value)
        {
            var sqlParameterCollection = new SqlParameterCollection();
            ValidateInput(ref key);
            ValidateInput(ref value);

            var queryBuilder = new StringBuilder("SELECT * FROM c");

            if (!string.IsNullOrEmpty(key))
            {
                queryBuilder.Append($" WHERE c[@keyProperty] = \"{value}\"");
                sqlParameterCollection.Add(new SqlParameter { Name = "@keyProperty", Value = key });
            }

            return new SqlQuerySpec(queryBuilder.ToString(), sqlParameterCollection);
        }

        public async Task<bool> CreateDatabaseIfNotExistsAsync(string cosmosDatabase, int docDBRUs)
        {
            bool results = false;
            try
            {
                this.requestOptions = this.GetDocDbOptions(docDBRUs);
                var uri = "/dbs/" + cosmosDatabase;
                await this.client.ReadDatabaseAsync(uri, this.requestOptions);
                results = true;
            }
            catch (DocumentClientException)
            {
                await this.CreateDatabaseAsync(cosmosDatabase);
            }

            return results;
        }

        public async Task<bool> CreateCollectionIfNotExistsAsync(string cosmosDatabase, string cosmosColection, int docDBRUs, CosmosOperation operation)
        {
            bool status = false;
            try
            {
                this.requestOptions = this.GetDocDbOptions(docDBRUs);
                var uri = $"/dbs/{cosmosDatabase}/colls/{cosmosColection}";
                await this.client.ReadDocumentCollectionAsync(uri, this.requestOptions);
                status = true;
            }
            catch (DocumentClientException)
            {
                await this.CreateCollectionAsync(cosmosDatabase, cosmosColection, docDBRUs, operation.ToString());
                status = true;
            }

            return status;
        }

        public async Task<ResourceResponse<Document>> SaveDocumentAsync(object payload, string collectionLink)
        {
            try
            {
                return await this.client.UpsertDocumentAsync(collectionLink, payload);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Unable to Upsert document with collection link {collectionLink}.", ex);
            }
        }

        public async Task<ValueServiceModel> SaveDocumentAsync(string collectionId, string key, ValueServiceModel input, string collectionLink)
        {
            try
            {
                var response = await this.client.UpsertDocumentAsync(
                    collectionLink,
                    new KeyValueDocument(collectionId, key, input.Data),
                    IfMatch(input.ETag));
                return new ValueServiceModel(response?.Resource);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Update failed while saving the resource", ex);
            }
        }

        public async Task DeleteDocumentAsync(string key, string collectionLink)
        {
            try
            {
                await this.client.DeleteDocumentAsync($"{collectionLink}/docs/{key}", new RequestOptions { PartitionKey = new PartitionKey(key) });
            }
            catch (DocumentClientException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Delete failed for the resource", ex);
            }
        }

        public async Task<ValueServiceModel> GetDocumentAsync(string collectionLink, string collectionId, string key)
        {
            try
            {
                var docId = KeyValueDocument.GenerateId(collectionId, key);
                var response = await this.client.ReadDocumentAsync($"{collectionLink}/docs/{docId}");
                return new ValueServiceModel(response?.Resource);
            }
            catch (Exception ex)
            {
                throw new ResourceNotFoundException($"The document at collection link {collectionLink} requested doesn't exist.", ex);
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

        public async Task<List<Document>> QueryAllDocumentsAsync(
            string databaseName,
            string colId,
            FeedOptions queryOptions,
            SqlQuerySpec querySpec)
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
                        .ToList();

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

        private static RequestOptions IfMatch(string condition)
        {
            if (condition == "*")
            {
                // Match all
                return null;
            }

            return new RequestOptions
            {
                AccessCondition = new AccessCondition
                {
                    Condition = condition,
                    Type = AccessConditionType.IfMatch,
                },
            };
        }

        private static CosmosOperations CreateInstance()
        {
            string connectionString = Environment.GetEnvironmentVariable("CosmosDbConnectionString", EnvironmentVariableTarget.Process);
            DocumentClient docClient = CreateClient(connectionString);
            return new CosmosOperations(docClient);
        }

        private static DocumentClient CreateClient(string docDBConnectionString)
        {
            string connectionStringRegex = "^AccountEndpoint=(?<endpoint>.*);AccountKey=(?<key>.*);$";
            try
            {
                var match = Regex.Match(docDBConnectionString, connectionStringRegex);
                if (!match.Success)
                {
                    string message = "Invalid Connection String for CosmosDb";
                    throw new Exception(message);
                }

                Uri docDbEndpoint = new Uri(match.Groups["endpoint"].Value);
                string docDbKey = match.Groups["key"].Value;
                return new DocumentClient(
                    docDbEndpoint,
                    docDbKey,
                    new ConnectionPolicy
                    {
                        ConnectionMode = ConnectionMode.Direct,
                        ConnectionProtocol = Protocol.Tcp,
                    });
            }
            catch (Exception ex)
            {
                throw new ApplicationException("DocumentClient creation failed in the helper class", ex);
            }
        }

        private static void ValidateInput(ref string input)
        {
            input = input.Trim();

            if (Regex.IsMatch(input, InvalidCharacterRegex))
            {
                throw new InvalidInputException($"Input '{input}' contains invalid characters.");
            }
        }

        private RequestOptions GetDocDbOptions(int docDBRUs)
        {
            return new RequestOptions
            {
                OfferThroughput = docDBRUs,
                ConsistencyLevel = ConsistencyLevel.Strong,
            };
        }

        private async Task CreateDatabaseAsync(string cosmosDatabase)
        {
            try
            {
                var db = new Database { Id = cosmosDatabase };
                await this.client.CreateDatabaseAsync(db);
            }
            catch (DocumentClientException)
            {
                // If the DB is already created it will throw a DocumentClientException and we do not want to handle that
                // So we do not throw this exception
            }
            catch (Exception e)
            {
                throw new ApplicationException("Error while creating DocumentDb database", e);
            }
        }

        private async Task CreateCollectionAsync(string cosmosDatabase, string cosmosCollection, int docDBRUs, string operation)
        {
            try
            {
                var collection = new DocumentCollection { Id = cosmosCollection };

                var index = Microsoft.Azure.Documents.Index.Range(DataType.String, -1);
                var indexing = new IndexingPolicy(index) { IndexingMode = IndexingMode.Consistent };
                collection.IndexingPolicy = indexing;

                if (operation.ToString().Equals("device"))
                {
                    collection.PartitionKey.Paths.Add("/deviceId");
                }

                var dbUri = "/dbs/" + cosmosDatabase;
                this.requestOptions = this.GetDocDbOptions(docDBRUs);
                await this.client.CreateDocumentCollectionAsync(dbUri, collection, this.requestOptions);
            }
            catch (DocumentClientException)
            {
                // If the collection is already created it will throw a DocumentClientException and we do not want to handle that
                // So we do not throw this exception
            }
            catch (Exception e)
            {
                throw new ApplicationException("Error while creating DocumentDb collection", e);
            }
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