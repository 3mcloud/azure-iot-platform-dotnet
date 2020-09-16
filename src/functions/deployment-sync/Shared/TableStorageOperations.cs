// <copyright file="TableStorageOperations.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class TableStorageOperations
    {
        private static TableStorageOperations instance = null;
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly CloudTableClient client = null;

        private TableStorageOperations(CloudTableClient cloudTableClient)
        {
            this.client = cloudTableClient;
        }

        public static async Task<TableStorageOperations> GetClientAsync()
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

        public async Task<T> InsertAsync<T>(string tableName, T entity)
            where T : ITableEntity
        {
            CloudTable table = await this.GetTableAsync(tableName);
            TableOperation insertOperation = TableOperation.Insert(entity);
            return await this.ExecuteTableOperationAsync<T>(table, insertOperation);
        }

        public async Task<T> InsertOrReplaceAsync<T>(string tableName, T entity)
            where T : ITableEntity
        {
            CloudTable table = await this.GetTableAsync(tableName);
            TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(entity);
            return await this.ExecuteTableOperationAsync<T>(table, insertOrReplaceOperation);
        }

        public async Task<T> InsertOrMergeAsync<T>(string tableName, T entity)
            where T : ITableEntity
        {
            CloudTable table = await this.GetTableAsync(tableName);
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            return await this.ExecuteTableOperationAsync<T>(table, insertOrMergeOperation);
        }

        public async Task<T> RetrieveAsync<T>(string tableName, string partitionKey, string rowKey)
            where T : ITableEntity
        {
            CloudTable table = await this.GetTableAsync(tableName);
            TableOperation readOperation = TableOperation.Retrieve<T>(partitionKey, rowKey);
            return await this.ExecuteTableOperationAsync<T>(table, readOperation);
        }

        public async Task<T> DeleteAsync<T>(string tableName, T entity)
            where T : ITableEntity
        {
            CloudTable table = await this.GetTableAsync(tableName);
            TableOperation deleteOperation = TableOperation.Delete(entity);
            return await this.ExecuteTableOperationAsync<T>(table, deleteOperation);
        }

        public async Task<List<T>> QueryAsync<T>(
            string tableName,
            TableQuery<T> query,
            CancellationToken cancellationToken = default(CancellationToken))
            where T : ITableEntity, new()
        {
            CloudTable table = await this.GetTableAsync(tableName);
            var items = new List<T>();
            TableContinuationToken token = null;
            do
            {
                // combine query segments until the full query has been executed or cancelled
                TableQuerySegment<T> seg = await table.ExecuteQuerySegmentedAsync<T>(query, token);
                token = seg.ContinuationToken;
                items.AddRange(seg);
            }
            while (token != null && !cancellationToken.IsCancellationRequested);
            return items;
        }

        private static TableStorageOperations CreateInstance()
        {
            string connectionString = Environment.GetEnvironmentVariable("tableStorageConnectionString", EnvironmentVariableTarget.Process);
            CloudTableClient tableClient = CreateClient(connectionString);
            return new TableStorageOperations(tableClient);
        }

        private static CloudTableClient CreateClient(string tableStorageConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(tableStorageConnectionString);
            return storageAccount.CreateCloudTableClient();
        }

        private async Task<T> ExecuteTableOperationAsync<T>(CloudTable table, TableOperation operation)
            where T : ITableEntity
        {
            try
            {
                TableResult result = await table.ExecuteAsync(operation);
                try
                {
                    return (T)result.Result;
                }
                catch (Exception e)
                {
                    throw new Exception($"Unable to transform the table result {result.ToString()} to the requested entity type", e);
                }
            }
            catch (StorageException se)
            {
                throw new Exception($"Unable to perform the requested table operation {operation.OperationType}", se);
            }
        }

        private async Task<CloudTable> GetTableAsync(string tableName)
        {
            try
            {
                CloudTable table = this.client.GetTableReference(tableName);
                try
                {
                    await table.CreateIfNotExistsAsync();
                }
                catch (StorageException e)
                {
                    throw new Exception($"An error occurred during table.CreateIfNotExistsAsync for the {tableName} table.", e);
                }

                return table;
            }
            catch (StorageException se)
            {
                throw new Exception($"An error occurred while attempting to get the {tableName} table and checking if it needed to be created.", se);
            }
        }
    }
}