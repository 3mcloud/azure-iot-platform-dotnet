// <copyright file="UserTenantController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mmm.Iot.Common.Services;
using Mmm.Iot.Common.Services.Filters;
using Mmm.Iot.IdentityGateway.Services;
using Mmm.Iot.IdentityGateway.Services.Helpers;
using Mmm.Iot.IdentityGateway.Services.Models;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Mmm.Iot.IdentityGateway.WebService.Controllers
{
    [Route("v1/tenants")]
    [TypeFilter(typeof(ExceptionsFilterAttribute))]
    public class UserTenantController : Controller
    {
        private readonly ISendGridClientFactory sendGridClientFactory;
        private UserTenantContainer container;
        private IJwtHelpers jwtHelper;
        private IInviteHelpers inviteHelper;
        private UserSettingsContainer settingsContainer;

        public UserTenantController(UserTenantContainer container, IJwtHelpers jwtHelper, ISendGridClientFactory sendGridClientFactory, IInviteHelpers inviteHelper, UserSettingsContainer settingsContainer)
        {
            this.container = container;
            this.jwtHelper = jwtHelper;
            this.sendGridClientFactory = sendGridClientFactory;
            this.inviteHelper = inviteHelper;
            this.settingsContainer = settingsContainer;
        }

        [HttpGet("users")]
        [Authorize("ReadAll")]
        public async Task<UserTenantListModel> GetAllUsersForTenantAsync()
        {
            UserTenantInput input = new UserTenantInput
            {
                UserId = null,
                Tenant = this.GetTenantId(),
            };
            return await this.container.GetAllUsersAsync(input);
        }

        [HttpGet("all")]
        public async Task<UserTenantListModel> UserClaimsGetAllTenantsForUserAsync()
        {
            return await this.GetAllTenantsForUserAsync(this.GetClaimsUserId());
        }

        [HttpGet("{userId}/all")]
        [Authorize("ReadAll")]
        public async Task<UserTenantListModel> GetAllTenantsForUserAsync(string userId)
        {
            UserTenantInput input = new UserTenantInput
            {
                UserId = userId,
            };
            return await this.container.GetAllAsync(input);
        }

        [HttpGet("")]
        [Authorize("ReadAll")]
        public async Task<UserTenantModel> UserClaimsGetAsync()
        {
            return await this.GetAsync(this.GetClaimsUserId());
        }

        [HttpGet("{userId}")]
        [Authorize("ReadAll")]
        public async Task<UserTenantModel> GetAsync(string userId)
        {
            UserTenantInput input = new UserTenantInput
            {
                UserId = userId,
                Tenant = this.GetTenantId(),
            };
            return await this.container.GetAsync(input);
        }

        [HttpPost("")]
        [Authorize("UserManage")]
        public async Task<UserTenantModel> UserClaimsPostAsync([FromBody] UserTenantModel model)
        {
            return await this.PostAsync(this.GetClaimsUserId(), model);
        }

        [HttpPost("{userId}")]
        [Authorize("UserManage")]
        public async Task<UserTenantModel> PostAsync(string userId, [FromBody] UserTenantModel model)
        {
            UserTenantInput input = new UserTenantInput
            {
                UserId = userId,
                Tenant = this.GetTenantId(),
                Roles = model.Roles,
                Name = model.Name,
                Type = string.IsNullOrWhiteSpace(model.Type) ? "Member" : model.Type,
            };
            return await this.container.CreateAsync(input);
        }

        [HttpPut("")]
        [Authorize("UserManage")]
        public async Task<UserTenantModel> UserClaimsPutAsync([FromBody] UserTenantModel update)
        {
            return await this.PutAsync(this.GetClaimsUserId(), update);
        }

        [HttpPut("{userId}")]
        [Authorize("UserManage")]
        public async Task<UserTenantModel> PutAsync(string userId, [FromBody] UserTenantModel update)
        {
            UserTenantInput input = new UserTenantInput
            {
                UserId = userId,
                Tenant = this.GetTenantId(),
                Roles = update.Roles,
            };
            return await this.container.UpdateAsync(input);
        }

        [HttpDelete("")]
        [Authorize("UserManage")]
        public async Task<UserTenantModel> UserClaimsDeleteAsync()
        {
            return await this.DeleteAsync(this.GetClaimsUserId());
        }

        [HttpDelete("{userId}")]
        [Authorize("UserManage")]
        public async Task<UserTenantModel> DeleteAsync(string userId)
        {
            UserTenantInput input = new UserTenantInput
            {
                UserId = userId,
                Tenant = this.GetTenantId(),
            };
            var userTenantModel = await this.container.DeleteAsync(input);
            if (userTenantModel != null)
            {
                await this.CleanupUserSettingsIfUserHasNoOtherTenants(userTenantModel);
            }

            return userTenantModel;
        }

        [HttpDelete("all")]
        [Authorize("UserManage")]
        public async Task<UserTenantListModel> DeleteAllAsync()
        {
            UserTenantInput input = new UserTenantInput
            {
                Tenant = this.GetTenantId(),
            };
            var userTenants = await this.container.DeleteAllAsync(input);
            if (userTenants != null && userTenants.Models != null && userTenants.Models.Any())
            {
                foreach (UserTenantModel userTenantModel in userTenants.Models)
                {
                    await this.CleanupUserSettingsIfUserHasNoOtherTenants(userTenantModel);
                }
            }

            return userTenants;
        }

        [HttpPost("invite")]
        [Authorize("UserManage")]
        public async Task<UserTenantModel> InviteAsync([FromBody] Invitation invitation)
        {
            // Object to insert in table as placeholder
            UserTenantInput input = new UserTenantInput
            {
                UserId = Guid.NewGuid().ToString(),
                Tenant = this.GetTenantId(),
                Roles = JsonConvert.SerializeObject(new List<string>() { invitation.Role }),
                Name = invitation.EmailAddress,
                Type = "Invited",
            };

            List<Claim> claims = new List<Claim>()
            {
                new Claim("role", invitation.Role),
                new Claim("tenant", this.GetTenantId()),
                new Claim("userId", input.UserId),
            };

            string forwardedFor = null;

            // add issuer with forwarded for address if exists (added by reverse proxy)
            if (this.HttpContext.Request.Headers.Where(t => t.Key == "X-Forwarded-For").Count() > 0)
            {
                forwardedFor = this.HttpContext.Request.Headers.Where(t => t.Key == "X-Forwarded-For").FirstOrDefault().Value
                    .First();
            }

            var jwtHandler = new JwtSecurityTokenHandler();
            string inviteToken = jwtHandler.WriteToken(this.jwtHelper.MintToken(claims, "IdentityGateway", DateTime.Now.AddDays(3)));

            var msg = this.inviteHelper.CreateMessage(invitation, forwardedFor, inviteToken, this.HttpContext.Request.Host.ToString());

            if (invitation.InviteUserMetaData != null)
            {
                await this.inviteHelper.CreateUserSettings(invitation, input.UserId);
            }

            // Send email
            var client = this.sendGridClientFactory.CreateSendGridClient();
            var response = await client.SendEmailAsync(msg);

            return await this.container.CreateAsync(input);
        }

        private async Task CleanupUserSettingsIfUserHasNoOtherTenants(UserTenantModel userTenantModel)
        {
            if (userTenantModel != null)
            {
                bool userHasOtherTenants = await this.DoesUserHaveAnyOtherTenants(userTenantModel.UserId);
                if (!userHasOtherTenants)
                {
                    UserSettingsInput settingsInput = new UserSettingsInput();
                    settingsInput.UserId = userTenantModel.UserId;
                    UserSettingsListModel userSettings = await this.settingsContainer.GetAllAsync(settingsInput);
                    if (userSettings != null && userSettings.Models != null)
                    {
                        foreach (UserSettingsModel settingsModel in userSettings.Models)
                        {
                            settingsInput.UserId = settingsModel.UserId;
                            settingsInput.SettingKey = settingsModel.SettingKey;
                            await this.settingsContainer.DeleteAsync(settingsInput);
                        }
                    }
                }
            }
        }

        private async Task<bool> DoesUserHaveAnyOtherTenants(string userId)
        {
            UserTenantInput input = new UserTenantInput
            {
                UserId = userId,
            };
            var otherTenantForUser = await this.container.GetAllAsync(input);
            if (otherTenantForUser != null && otherTenantForUser.Models != null && otherTenantForUser.Models.Any())
            {
                return true;
            }

            return false;
        }
    }
}