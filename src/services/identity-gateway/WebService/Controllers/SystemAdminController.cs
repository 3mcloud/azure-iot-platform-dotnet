// <copyright file="SystemAdminController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mmm.Iot.Common.Services.Filters;
using Mmm.Iot.IdentityGateway.Services;
using Mmm.Iot.IdentityGateway.Services.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.IdentityGateway.WebService.Controllers
{
    [Route("v1/systemAdmin")]
    [TypeFilter(typeof(ExceptionsFilterAttribute))]
    public class SystemAdminController : Controller
    {
        private const string MemberType = "Member";
        private const string AdminRole = "admin";
        private SystemAdminContainer container;
        private UserTenantContainer userTenantcontainer;
        private AuthorizeContainer authorizeContainer;

        public SystemAdminController(SystemAdminContainer container, UserTenantContainer userTenantcontainer, AuthorizeContainer authorizeContainer)
        {
            this.container = container;
            this.userTenantcontainer = userTenantcontainer;
            this.authorizeContainer = authorizeContainer;
        }

        [HttpPost]
        [SystemAdminAuthorize]
        public async Task<SystemAdminModel> CreateSystemAdminAsync([FromBody] UserModel model)
        {
            SystemAdminInput systemAdminInput = new SystemAdminInput
            {
                UserId = model.UserId,
                Name = model.Name,
            };

            var result = await this.container.CreateAsync(systemAdminInput);
            if (result != null)
            {
                await this.AddUserForPendingTenants(result);
            }

            return result;
        }

        [HttpGet("getAllNonSystemAdmins")]
        [SystemAdminAuthorize]
        public async Task<UserListModel> GetAllNonSystemAdminsAsync()
        {
            var allUsers = await this.userTenantcontainer.GetAllUsersAsync();

            var existingSystemAdmins = await this.container.GetAllAsync();

            if (existingSystemAdmins != null && existingSystemAdmins.Models != null && existingSystemAdmins.Models.Count() > 0)
            {
                var existingSystemAdminIds = existingSystemAdmins.Models.Select(s => s.PartitionKey);

                allUsers.Models.RemoveAll(s => existingSystemAdminIds.Contains(s.UserId));
            }

            return allUsers;
        }

        [HttpGet("getAllSystemAdmins")]
        [SystemAdminAuthorize]
        public async Task<UserListModel> GetAllSystemAdminsAsync()
        {
            var existingSystemAdmins = await this.container.GetAllAsync();
            UserListModel usersList = new UserListModel();
            usersList.Models = new List<UserModel>();
            if (existingSystemAdmins.Models != null)
            {
                foreach (var systemAdmin in existingSystemAdmins.Models)
                {
                    usersList.Models.Add(
                        new UserModel()
                        {
                            UserId = systemAdmin.PartitionKey,
                            Name = systemAdmin.Name,
                        });
                }
            }

            return usersList;
        }

        [HttpDelete("{userId}")]
        [SystemAdminAuthorize]
        public async Task<SystemAdminModel> DeleteSystemAdminAsync(string userId)
        {
            SystemAdminInput systemAdminInput = new SystemAdminInput
            {
                UserId = userId,
            };
            return await this.container.DeleteAsync(systemAdminInput);
        }

        [HttpPost("AuthProvider/{authProvider}")]
        [Authorize("ReadAll")]
        public async Task<bool> UpdateAuthProvider(string authProvider)
        {
            return await this.authorizeContainer.UpdateAuthProvider(authProvider);
        }

        private async Task AddUserForPendingTenants(SystemAdminModel result)
        {
            if (result != null)
            {
                TenantListModel activeTenants = await this.userTenantcontainer.GetAllActiveTenantAsync();
                if (activeTenants != null && activeTenants.Models != null && activeTenants.Models.Count > 0)
                {
                    List<string> activeTenantIds = activeTenants.Models.Select(x => x.TenantId).ToList();
                    if (activeTenantIds != null && activeTenantIds.Count > 0)
                    {
                        UserTenantInput userInput = new UserTenantInput()
                        {
                            UserId = result.PartitionKey,
                            Name = result.Name,
                            Roles = JsonConvert.SerializeObject(new List<string>() { AdminRole }),
                            Type = MemberType,
                        };
                        UserTenantListModel existingTenants = await this.userTenantcontainer.GetAllAsync(userInput);
                        for (int i = 0; i < activeTenantIds.Count; i++)
                        {
                            userInput.Tenant = activeTenantIds[i];
                            if (existingTenants != null && existingTenants.Models != null &&
                                existingTenants.Models.FirstOrDefault(x => x.UserId == userInput.UserId && x.TenantId == userInput.Tenant) == null)
                            {
                                var createdUser = await this.userTenantcontainer.CreateAsync(userInput);
                            }
                        }
                    }
                }
            }
        }
    }
}