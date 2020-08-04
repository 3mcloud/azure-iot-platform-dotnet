// <copyright file="UserTenantContainer.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.External.TableStorage;
using Mmm.Iot.IdentityGateway.Services.External;
using Mmm.Iot.IdentityGateway.Services.Models;

namespace Mmm.Iot.IdentityGateway.Services
{
    public class UserTenantContainer : UserContainer, IUserContainer<UserTenantModel, UserTenantInput>
    {
        private readonly ILogger logger;
        private readonly ITenantManagerClient tenantManagerClient;

        public UserTenantContainer(ILogger<UserTenantContainer> logger)
        {
            this.logger = logger;
        }

        public UserTenantContainer(ITableStorageClient tableStorageClient, ILogger<UserTenantContainer> logger, ITenantManagerClient tenantManagerClient)
            : base(tableStorageClient)
        {
            this.logger = logger;
            this.tenantManagerClient = tenantManagerClient;
        }

        public override string TableName => "user";

        public string TenantTableId => "tenant";

        public virtual async Task<UserTenantListModel> GetAllAsync(UserTenantInput input)
        {
            TableQuery<UserTenantModel> query = new TableQuery<UserTenantModel>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, input.UserId));
            List<UserTenantModel> result = await this.TableStorageClient.QueryAsync<UserTenantModel>(this.TableName, query);
            return new UserTenantListModel("GetTenants", result);
        }

        public virtual async Task<UserTenantListModel> GetAllUsersAsync(UserTenantInput input)
        {
            TableQuery<UserTenantModel> query = new TableQuery<UserTenantModel>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, input.Tenant));
            List<UserTenantModel> result = await this.TableStorageClient.QueryAsync<UserTenantModel>(this.TableName, query);
            return new UserTenantListModel("GetUsers", result);
        }

        public virtual async Task<UserTenantModel> GetAsync(UserTenantInput input)
        {
            return await this.TableStorageClient.RetrieveAsync<UserTenantModel>(this.TableName, input.UserId, input.Tenant);
        }

        public virtual async Task<UserTenantModel> CreateAsync(UserTenantInput input)
        {
            // In order to create a new user with a tenant, create a new user id
            if (input.UserId == null)
            {
                input.UserId = Guid.NewGuid().ToString();
            }

            // Create the user and options for creating the user record in the user table
            UserTenantModel existingModel = await this.GetAsync(input);
            if (existingModel != null)
            {
                string errorMessage = $"That UserTenant record already exists with value {existingModel.Roles}." + " Use PUT instead to update this setting instead.";
                StorageException exception = new StorageException(errorMessage);
                this.logger.LogError(exception, errorMessage);

                // If this record already exists, return it without continuing with the insert operation
                throw exception;
            }

            UserTenantModel user = new UserTenantModel(input);
            return await this.TableStorageClient.InsertAsync(this.TableName, user);
        }

        public virtual async Task<UserTenantModel> UpdateAsync(UserTenantInput input)
        {
            UserTenantModel model = new UserTenantModel(input);
            if (model.RoleList != null && !model.RoleList.Any())
            {
                string errorMessage = "The UserTenant update model must contain a serialized role array.";
                ArgumentException exception = new ArgumentException(errorMessage);
                this.logger.LogError(exception, errorMessage);

                // If the RoleList of the model is empty, throw an exception. The RoleList is the only updateable feature of the UserTenant Table
                throw exception;
            }

            model.ETag = "*";  // An ETag is required for updating - this allows any etag to be used
            return await this.TableStorageClient.InsertOrMergeAsync(this.TableName, model);
        }

        public virtual async Task<UserTenantModel> DeleteAsync(UserTenantInput input)
        {
            // Get a list of all user models for this user id - we will pick the one matching the current tenant to delete
            UserTenantModel user = await this.GetAsync(input);
            if (user == null)
            {
                string errorMessage = $"That UserTenant does not exist";
                StorageException exception = new StorageException(errorMessage);
                this.logger.LogError(exception, errorMessage);
                throw exception;
            }

            user.ETag = "*";  // An ETag is required for deleting - this allows any etag to be used
            return await this.TableStorageClient.DeleteAsync(this.TableName, user);
        }

        public virtual async Task<UserTenantListModel> DeleteAllAsync(UserTenantInput input)
        {
            UserTenantListModel tenantRows = await this.GetAllUsersAsync(input);

            // delete all rows as one asynchronous job
            var deleteTasks = tenantRows.Models.Select(row =>
            {
                UserTenantInput deleteInput = new UserTenantInput
                {
                    UserId = row.PartitionKey,
                    Tenant = input.Tenant,
                };
                return this.DeleteAsync(deleteInput);
            });
            var deletionResult = await Task.WhenAll(deleteTasks);
            return new UserTenantListModel("Delete", deletionResult.ToList());
        }

        public virtual async Task<bool> CheckAnyUserExist()
        {
            TableQuery<UserTenantModel> query = new TableQuery<UserTenantModel>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.NotEqual, string.Empty)).Take(1);
            List<UserTenantModel> result = await this.TableStorageClient.QueryAsync<UserTenantModel>(this.TableName, query);
            return result.Count > 0;
        }

        public virtual async Task<UserListModel> GetAllUsersAsync()
        {
            TableQuery<UserTenantModel> query = new TableQuery<UserTenantModel>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.NotEqual, string.Empty));
            List<UserTenantModel> result = await this.TableStorageClient.QueryAsync<UserTenantModel>(this.TableName, query);
            Dictionary<string, string> users = new Dictionary<string, string>();
            foreach (var userTenant in result.Where(x => x.Type == "Member" || string.IsNullOrWhiteSpace(x.Type)))
            {
                if (!users.ContainsKey(userTenant.PartitionKey))
                {
                    users.Add(userTenant.PartitionKey, userTenant.Name);
                }
            }

            List<UserModel> allUsers = new List<UserModel>();
            foreach (KeyValuePair<string, string> entry in users)
            {
                allUsers.Add(new UserModel(entry.Key, entry.Value));
            }

            return new UserListModel("GetAllUsers", allUsers);
        }

        public virtual async Task<TenantListModel> GetAllActiveTenantAsync()
        {
            try
            {
                // Load the tenant from table storage
                TableQuery<TenantModel> query = new TableQuery<TenantModel>().Where(TableQuery.GenerateFilterConditionForBool("IsIotHubDeployed", QueryComparisons.Equal, true));
                List<TenantModel> result = await this.TableStorageClient.QueryAsync<TenantModel>(this.TenantTableId, query);
                return new TenantListModel(result);
            }
            catch (Exception e)
            {
                throw new Exception("Unable to retrieve the active tenants from table storage", e);
            }
        }

        public async Task<TenantListModel> GetAllActiveTenantAsync()
        {
            try
            {
                // Load the tenant from table storage
                TableQuery<TenantModel> query = new TableQuery<TenantModel>().Where(TableQuery.GenerateFilterConditionForBool("IsIotHubDeployed", QueryComparisons.Equal, true));
                List<TenantModel> result = await this.TableStorageClient.QueryAsync<TenantModel>(this.TenantTableId, query);
                return new TenantListModel(result);
            }
            catch (Exception e)
            {
                throw new Exception("Unable to retrieve the active tenants from table storage", e);
            }
        }

        public async Task<TenantListModel> GetAllActiveTenantAsync()
        {
            return await this.tenantManagerClient.GetAllActiveTenantAsync();
        }
    }
}