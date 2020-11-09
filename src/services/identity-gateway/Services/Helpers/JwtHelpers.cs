// <copyright file="JwtHelpers.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using IdentityModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.IdentityGateway.Services.Models;

namespace Mmm.Iot.IdentityGateway.Services.Helpers
{
    public class JwtHelpers : IJwtHelpers
    {
        private readonly IOpenIdProviderConfiguration openIdProviderConfiguration;
        private readonly IRsaHelpers rsaHelpers;
        private readonly ILogger logger;
        private UserTenantContainer userTenantContainer;
        private UserSettingsContainer userSettingsContainer;
        private SystemAdminContainer systemAdminContainer;
        private AppConfig config;
        private IHttpContextAccessor httpContextAccessor;

        public JwtHelpers(
                        UserTenantContainer userTenantContainer,
                        UserSettingsContainer userSettingsContainer,
                        SystemAdminContainer systemAdminContainer,
                        AppConfig config,
                        IHttpContextAccessor httpContextAccessor,
                        IOpenIdProviderConfiguration openIdProviderConfiguration,
                        IRsaHelpers rsaHelpers,
                        ILogger<JwtHelpers> logger)
        {
            this.userTenantContainer = userTenantContainer;
            this.userSettingsContainer = userSettingsContainer;
            this.config = config;
            this.httpContextAccessor = httpContextAccessor;
            this.openIdProviderConfiguration = openIdProviderConfiguration;
            this.rsaHelpers = rsaHelpers;
            this.logger = logger;
            this.systemAdminContainer = systemAdminContainer;
        }

        public async Task<JwtSecurityToken> GetIdentityToken(List<Claim> claims, string tenant, string audience, DateTime? expiration)
        {
            // add iat claim
            var timeSinceEpoch = DateTime.UtcNow.ToEpochTime();
            claims.Add(new Claim("iat", timeSinceEpoch.ToString(), ClaimValueTypes.Integer));

            var userId = claims.First(t => t.Type == "sub").Value;

            // Create a userTenantInput for the purpose of finding the full tenant list associated with this user
            UserTenantInput tenantInput = new UserTenantInput
            {
                UserId = userId,
            };
            UserTenantListModel tenantsModel = await this.userTenantContainer.GetAllAsync(tenantInput);
            List<UserTenantModel> tenantList = tenantsModel.Models;

            // User did not specify the tenant to log into so get the default or last used
            if (string.IsNullOrEmpty(tenant))
            {
                // authState has no tenant, so we should use either the User's last used tenant, or the first tenant available to them
                // Create a UserSettingsInput for the purpose of finding the LastUsedTenant setting for this user
                this.logger.LogInformation("User did not specify Tenant so default/last used tenant is set.");
                UserSettingsInput settingsInput = new UserSettingsInput
                {
                    UserId = userId,
                    SettingKey = "LastUsedTenant",
                };
                UserSettingsModel lastUsedSetting = await this.userSettingsContainer.GetAsync(settingsInput);

                // Has last used tenant and it is in the list
                if (lastUsedSetting != null && tenantList.Count(t => t.TenantId == lastUsedSetting.Value) > 0)
                {
                    tenant = lastUsedSetting.Value;
                }

                if (string.IsNullOrEmpty(tenant) && tenantList.Count > 0)
                {
                    tenant =
                        tenantList.First()
                            .TenantId; // Set the tenant to the first tenant in the list of tenants for this user
                }
            }

            // If User not associated with Tenant then dont add claims return token without
            if (tenant != null)
            {
                UserTenantInput input = new UserTenantInput
                {
                    UserId = userId,
                    Tenant = tenant,
                };
                UserTenantModel tenantModel = await this.userTenantContainer.GetAsync(input);

                // Add Tenant
                claims.Add(new Claim("tenant", tenantModel.TenantId));

                // Add Roles
                tenantModel.RoleList.ForEach(role => claims.Add(new Claim("role", role)));

                // Settings Update LastUsedTenant
                UserSettingsInput settingsInput = new UserSettingsInput
                {
                    UserId = claims.Where(c => c.Type == "sub").First().Value,
                    SettingKey = "LastUsedTenant",
                    Value = tenant,
                };

                // Update if name is not the same
                await this.userSettingsContainer.UpdateAsync(settingsInput);
                if (tenantModel.Name != claims.Where(c => c.Type == "name").First().Value)
                {
                    input.Name = claims.Where(c => c.Type == "name").First().Value;
                    await this.userTenantContainer.UpdateAsync(input);
                }
            }

            // verify the current user against systemAdmin collection to determine whether he is a systemAdmin.
            await this.GenerateSystemAdminClaim(claims);

            DateTime expirationDateTime = expiration ?? DateTime.Now.AddDays(30);

            // add all tenants they have access to
            claims.AddRange(tenantList.Select(t => new Claim("available_tenants", t.TenantId)));

            // Token to String so you can use it in your client
            var token = this.MintToken(claims, audience, expirationDateTime);

            return token;
        }

        [SuppressMessage("Microsoft.StyleCop.CSharp.OrderingRules", "SA1121:UseBuiltInTypeAlias", Justification = "I need that Byte value not lowercase byte")]
        public string AtHash(String access_token)
        {
            SHA256Managed hashstring = new SHA256Managed();
            byte[] bytes = Encoding.ASCII.GetBytes(access_token);
            byte[] hash = hashstring.ComputeHash(bytes);
            Byte[] sixteen_bytes = new Byte[16];
            Array.Copy(hash, sixteen_bytes, 16);
            return WebEncoders.Base64UrlEncode(sixteen_bytes).Trim('=');
        }

        public JwtSecurityToken MintToken(List<Claim> claims, string audience, DateTime expirationDateTime)
        {
            // Create Security key  using private key above:
            // not that latest version of JWT using Microsoft namespace instead of System
            var securityKey =
                new RsaSecurityKey(this.rsaHelpers.DecodeRsa(this.config.IdentityGatewayService.PrivateKey));

            // Also note that securityKey length should be >256b
            // so you have to make sure that your private key has a proper length
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: this.DeriveIssuer(),
                audience: audience,
                expires: expirationDateTime.ToUniversalTime(),
                claims: claims.ToArray(),
                signingCredentials: credentials);
            return token;
        }

        public bool TryValidateToken(string audience, string encodedToken, HttpContext context, out JwtSecurityToken jwt)
        {
            jwt = null;
            var jwtHandler = new JwtSecurityTokenHandler();
            if (!jwtHandler.CanReadToken(encodedToken))
            {
                string errorMessage = "Cannot read token validation failed";
                this.logger.LogError(new Exception(errorMessage), errorMessage);
                return false;
            }

            var tokenValidationParams = new TokenValidationParameters
            {
                // Validate the token signature
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = this.rsaHelpers.GetJsonWebKey(this.config.IdentityGatewayService.PublicKey).Keys,

                // Validate the token issuer
                ValidateIssuer = false,
                ValidIssuer = this.openIdProviderConfiguration.Issuer,

                // Validate the token audience
                ValidateAudience = false,
                ValidAudience = audience,

                // Validate token lifetime
                ValidateLifetime = true,
                ClockSkew = new TimeSpan(0), // shouldnt be skewed as this is the same server that issued it.
            };

            SecurityToken validated_token = null;
            jwtHandler.ValidateToken(encodedToken, tokenValidationParams, out validated_token);
            if (validated_token == null)
            {
                string errorMessage = "Validate Token method failed to return valid value.";
                this.logger.LogError(new Exception(errorMessage), errorMessage);
                return false;
            }

            jwt = jwtHandler.ReadJwtToken(encodedToken);
            return true;
        }

        private string DeriveIssuer()
        {
            // Add issuer with forwarded for address if exists (added by reverse proxy)
            var issuer = this.httpContextAccessor.HttpContext.Request.Headers.FirstOrDefault(h => h.Key == "X-Forwarded-For").Value.FirstOrDefault();
            if (!string.IsNullOrEmpty(issuer))
            {
                this.logger.LogDebug("Deriving issuer '{issuer}' from X-Forwarded-For header in request", issuer);
            }
            else
            {
                issuer = $"{this.httpContextAccessor.HttpContext.Request.Scheme}://{this.httpContextAccessor.HttpContext.Request.Host.ToString()}";
                this.logger.LogDebug("Deriving issuer '{issuer}' from request scheme '{scheme}' and host '{host}'", issuer, this.httpContextAccessor.HttpContext.Request.Scheme, this.httpContextAccessor.HttpContext.Request.Host.ToString());
            }

            return issuer;
        }

        private async Task GenerateSystemAdminClaim(List<Claim> claims)
        {
            string userName = claims.Where(c => c.Type == "name").First().Value;
            string userId = claims.Where(c => c.Type == "sub").First().Value;
            string systemAdminClaim = "is_systemAdmin";

            SystemAdminInput systemAdminInput = new SystemAdminInput(userId, userName);
            SystemAdminListModel systemAdminListModel = await this.systemAdminContainer.GetAsync(systemAdminInput);
            if (systemAdminListModel.Models != null && systemAdminListModel.Models.Count() > 0)
            {
                claims.Add(new Claim(systemAdminClaim, true.ToString(), ClaimValueTypes.Boolean.ToString()));
            }
            else
            {
                claims.Add(new Claim(systemAdminClaim, false.ToString(), ClaimValueTypes.Boolean.ToString()));
            }
        }
    }
}