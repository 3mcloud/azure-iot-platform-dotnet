// <copyright file="GraphUserContainer.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Mmm.Iot.Common.Services.External.Graph;
using Mmm.Iot.Common.Services.External.TableStorage;
using Mmm.Iot.IdentityGateway.Services.Models;

namespace Mmm.Iot.IdentityGateway.Services
{
    public class GraphUserContainer : UserContainer
    {
        private readonly IGraphClient graphClient;
        private readonly ILogger logger;
        private readonly ITableStorageClient tableStorageClient;

        public GraphUserContainer()
        {
        }

        public GraphUserContainer(ILogger<GraphUserContainer> logger)
        {
            this.logger = logger;
        }

        public GraphUserContainer(IGraphClient graphClient, ITableStorageClient tableStorageClient, ILogger<GraphUserContainer> logger)
            : base(tableStorageClient)
        {
            this.graphClient = graphClient;
            this.logger = logger;
            this.tableStorageClient = tableStorageClient;
        }

        public override string TableName => "user";

        public async Task<UserTenantListModel> GetAllUsersAsync(UserTenantInput input)
        {
            TableQuery<UserTenantModel> query = new TableQuery<UserTenantModel>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, input.Tenant));

            List<UserTenantModel> users = await this.tableStorageClient.QueryAsync<UserTenantModel>(this.TableName, query);

            if (users.Count > 0)
            {
                List<User> usersInfo = new List<User>();
                for (int i = 0; i < users.Count; i = i + 15)
                {
                    var items = users.Skip(i).Take(15);
                    var usersInfoFromAd = await this.GetUsersByUserIdsAsync(items.Select(r => r.UserId));
                    if (usersInfoFromAd != null && usersInfoFromAd.Count > 0)
                    {
                        usersInfo.AddRange(usersInfoFromAd);
                    }
                }

                foreach (var user in users)
                {
                    User userInfo = usersInfo?.FirstOrDefault(u => u.Id == user.UserId);

                    if (userInfo != null)
                    {
                        user.Email = userInfo.OtherMails != null && userInfo.OtherMails.Count() > 0 ? userInfo.OtherMails.First() : string.Empty;
                        user.PhoneNumber = userInfo.MobilePhone;
                        user.Street = userInfo.StreetAddress;
                        user.City = userInfo.City;
                        user.PostalCode = userInfo.PostalCode;
                    }
                }
            }

            return new UserTenantListModel(users);
        }

        public async Task<IList<User>> GetUsersByUserIdsAsync(IEnumerable<string> userIds)
        {
            List<User> allUsers = new List<User>();
            if (userIds == null || userIds.Count() == 0)
            {
                return allUsers;
            }

            string query = $"id in [{string.Join(',', userIds.Select(u => $"'{u}'"))}]";

            var users = await this.graphClient.GetClient().Users
                    .Request()
                    .Filter(query)
                    .Select(e => new
                    {
                        e.DisplayName,
                        e.Id,
                        e.OtherMails,
                        e.MobilePhone,
                        e.StreetAddress,
                        e.City,
                        e.PostalCode,
                        e.CreationType,
                    })
                    .GetAsync();

            while (users.Count > 0)
            {
                allUsers.AddRange(users);
                if (users.NextPageRequest != null)
                {
                    users = await users.NextPageRequest
                        .GetAsync();
                }
                else
                {
                    break;
                }
            }

            return allUsers;
        }

        public async Task<User> GetUsersById(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var result = await this.graphClient.GetClient().Users[userId]
                    .Request()
                    .Select(e => new
                    {
                        e.DisplayName,
                        e.Id,
                        e.OtherMails,
                        e.MobilePhone,
                        e.StreetAddress,
                        e.City,
                        e.PostalCode,
                        e.CreationType,
                    })
                    .GetAsync();

            return result;
        }

        public async Task<User> UpdateUser(string userId, UserInfoPatch userInfoPatch)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new Exception("UserId is required");
            }

            // var user = await this.graphClient.GetClient().Users[userId]
            //        .Request()
            //        .Select(e => new
            //        {
            //            e.OtherMails,
            //            e.MobilePhone,
            //            e.StreetAddress,
            //            e.City,
            //            e.PostalCode,
            //        })
            //        .GetAsync();

            // if (user != null)
            // {
            User user = null;
            if (userInfoPatch.AlternateEmails.Count > 0)
            {
                user = new User
                {
                    OtherMails = userInfoPatch.AlternateEmails,
                };
            }

            /*foreach (var email in userInfoPatch.AlternateEmails)
            {
                user.OtherMails.Append(email);
            }*/

            // }
            if (!string.IsNullOrEmpty(userInfoPatch.MobilePhone))
            {
                if (user != null)
                {
                    user.MobilePhone = userInfoPatch.MobilePhone;
                }
                else
                {
                    user = new User
                    {
                        MobilePhone = userInfoPatch.MobilePhone,
                    };
                }
            }

            if (!string.IsNullOrEmpty(userInfoPatch.City))
            {
                if (user != null)
                {
                    user.City = userInfoPatch.City;
                }
                else
                {
                    user = new User
                    {
                        City = userInfoPatch.City,
                    };
                }
            }

            if (!string.IsNullOrEmpty(userInfoPatch.StreetAddress))
            {
                if (user != null)
                {
                    user.StreetAddress = userInfoPatch.StreetAddress;
                }
                else
                {
                    user = new User
                    {
                        StreetAddress = userInfoPatch.StreetAddress,
                    };
                }
            }

            if (!string.IsNullOrEmpty(userInfoPatch.PostalCode))
            {
                if (user != null)
                {
                    user.PostalCode = userInfoPatch.PostalCode;
                }
                else
                {
                    user = new User
                    {
                        PostalCode = userInfoPatch.PostalCode,
                    };
                }
            }

            var updatedUser = await this.graphClient.GetClient().Users[userId]
                 .Request()
                 .UpdateAsync(user);

            return updatedUser;
        }
    }
}