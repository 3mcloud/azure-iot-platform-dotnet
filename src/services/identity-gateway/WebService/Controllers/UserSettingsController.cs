// <copyright file="UserSettingsController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mmm.Iot.Common.Services;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Filters;
using Mmm.Iot.IdentityGateway.Services;
using Mmm.Iot.IdentityGateway.Services.Models;

namespace Mmm.Iot.IdentityGateway.WebService.Controllers
{
    [Route("v1/settings")]
    [TypeFilter(typeof(ExceptionsFilterAttribute))]
    [Authorize("ReadAll")]
    public class UserSettingsController : Controller
    {
        private UserSettingsContainer container;
        private AppConfig config;

        public UserSettingsController(UserSettingsContainer container)
        {
            this.container = container;
        }

        public UserSettingsController(UserSettingsContainer container, AppConfig config)
        {
            this.container = container;
            this.config = config;
        }

        [HttpGet("all")]
        public async Task<UserSettingsListModel> UserClaimsGetAllAsync()
        {
            return await this.GetAllAsync(this.GetClaimsUserId());
        }

        [HttpGet("{userId}/all")]
        public async Task<UserSettingsListModel> GetAllAsync(string userId)
        {
            UserSettingsInput input = new UserSettingsInput
            {
                UserId = userId,
            };
            return await this.container.GetAllAsync(input);
        }

        [HttpGet("{setting}")]
        public async Task<UserSettingsModel> UserClaimsGetAsync(string setting)
        {
            return await this.GetAsync(this.GetClaimsUserId(), setting);
        }

        [HttpGet("ActiveDeviceGroup")]
        public async Task<UserSettingsModel> GetUserActiveDeviceGroupAsync()
        {
            return await this.GetAsync(this.GetClaimsUserId(), this.GetUserActiveDeviceGroupKey());
        }

        [HttpGet("{userId}/{setting}")]
        public async Task<UserSettingsModel> GetAsync(string userId, string setting)
        {
            UserSettingsInput input = new UserSettingsInput
            {
                UserId = userId,
                SettingKey = setting,
            };
            return await this.container.GetAsync(input);
        }

        [HttpPost("{setting}/{value}")]
        [Authorize("UserManage")]
        public async Task<UserSettingsModel> UserClaimsPostAsync(string setting, string value)
        {
            return await this.PostAsync(this.GetClaimsUserId(), setting, value);
        }

        [HttpPost("{userId}/{setting}/{value}")]
        [Authorize("UserManage")]
        public async Task<UserSettingsModel> PostAsync(string userId, string setting, string value)
        {
            UserSettingsInput input = new UserSettingsInput
            {
                UserId = userId,
                SettingKey = setting,
                Value = value,
            };
            return await this.container.CreateAsync(input);
        }

        [HttpPut("{setting}/{value}")]
        [Authorize("UserManage")]
        public async Task<UserSettingsModel> UserClaimsPutAsync(string setting, string value)
        {
            return await this.PutAsync(this.GetClaimsUserId(), setting, value);
        }

        [HttpPut("ActiveDeviceGroup/{value}")]
        [Authorize("UserManage")]
        public async Task<UserSettingsModel> PutUserActiveDeviceGroupAsync(string value)
        {
            return await this.PutAsync(this.GetClaimsUserId(), this.GetUserActiveDeviceGroupKey(), value);
        }

        [HttpPut("{userId}/{setting}/{value}")]
        [Authorize("UserManage")]
        public async Task<UserSettingsModel> PutAsync(string userId, string setting, string value)
        {
            UserSettingsInput input = new UserSettingsInput
            {
                UserId = userId,
                SettingKey = setting,
                Value = value,
            };
            return await this.container.UpdateAsync(input);
        }

        [HttpDelete("{setting}")]
        [Authorize("UserManage")]
        public async Task<UserSettingsModel> UserClaimsDeleteAsync(string setting)
        {
            return await this.DeleteAsync(this.GetClaimsUserId(), setting);
        }

        [HttpDelete("{userId}/{setting}")]
        [Authorize("UserManage")]
        public async Task<UserSettingsModel> DeleteAsync(string userId, string setting)
        {
            UserSettingsInput input = new UserSettingsInput
            {
                UserId = userId,
                SettingKey = setting,
            };
            return await this.container.DeleteAsync(input);
        }

        [HttpGet("LatestDeploymentDate")]
        public UserSettingsModel GetLatestDeploymentDate()
        {
            return new UserSettingsModel() { Value = this.config.Global.LatestDeploymentDate };
        }

        private string GetUserActiveDeviceGroupKey()
        {
            return $"{this.GetTenantId()}-ActiveDeviceGroup";
        }
    }
}