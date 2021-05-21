// <copyright file="AuthorizeController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.Filters;
using Mmm.Iot.IdentityGateway.Services;
using Mmm.Iot.IdentityGateway.Services.Helpers;
using Mmm.Iot.IdentityGateway.Services.Models;
using Mmm.Iot.IdentityGateway.WebService.Models;
using Newtonsoft.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
namespace Mmm.Iot.IdentityGateway.Controllers
{
    [Route("")]
    [TypeFilter(typeof(ExceptionsFilterAttribute))]
    public class AuthorizeController : Controller
    {
        private readonly IOpenIdProviderConfiguration openIdProviderConfiguration;
        private AppConfig config;
        private IJwtHelpers jwtHelper;
        private IAuthenticationContext authenticationContext;
        private UserTenantContainer userTenantContainer;
        private UserSettingsContainer userSettingsContainer;

        public AuthorizeController(AppConfig config, UserTenantContainer userTenantContainer, UserSettingsContainer userSettingsContainer, IJwtHelpers jwtHelper, IOpenIdProviderConfiguration openIdProviderConfiguration, IAuthenticationContext authenticationContext)
        {
            this.config = config;
            this.userTenantContainer = userTenantContainer;
            this.userSettingsContainer = userSettingsContainer;
            this.jwtHelper = jwtHelper;
            this.openIdProviderConfiguration = openIdProviderConfiguration;
            this.authenticationContext = authenticationContext;
        }

        [HttpGet]
        [Route("connect/authorize")]
        public IActionResult Get(
            [FromQuery] string redirect_uri,
            [FromQuery] string state,
            [FromQuery(Name = "client_id")] string clientId,
            [FromQuery] string nonce,
            [FromQuery] string tenant,
            [FromQuery] string invite)
        {
            // Validate Input
            if (!Uri.IsWellFormedUriString(redirect_uri, UriKind.Absolute))
            {
                throw new Exception("Redirect Uri is not valid!");
            }

            Guid validatedGuid = Guid.Empty;

            // if is not null we want to validate that it is a guid, Otherwise it will pick a tenant for the user
            if (tenant != null && !Guid.TryParse(tenant, out validatedGuid))
            {
                throw new Exception("Tenant is not valid!");
            }

            var uri = new UriBuilder(this.config.Global.AzureB2cBaseUri);

            // Need to build Query carefully to not clobber other query items -- just injecting state
            var query = HttpUtility.ParseQueryString(uri.Query);
            query["state"] = JsonConvert.SerializeObject(new AuthState
            { ReturnUrl = redirect_uri, State = state, Tenant = tenant, Nonce = nonce, ClientId = clientId, Invitation = invite });
            query["redirect_uri"] = this.openIdProviderConfiguration.Issuer + "/connect/callback"; // must be https for B2C
            uri.Query = query.ToString();
            return this.Redirect(uri.Uri.ToString());
        }

        [HttpPost]
        [Route("connect/token")]
        public async Task<IActionResult> PostTokenAsync(
            [FromBody] ClientCredentialInput input)
        {
            string resourceUri = "https://graph.microsoft.com/";
            ClientCredential clientCredential = new ClientCredential(input.ClientId, input.ClientSecret);

            try
            {
                AuthenticationResult token = await this.authenticationContext.AcquireTokenAsync(resourceUri, clientCredential);
            }
            catch (Exception e)
            {
                return this.StatusCode(401, e.Message);
            }

            UserTenantInput tenantInput = new UserTenantInput
            {
                UserId = input.ClientId,
                Tenant = input.Scope,
            };
            UserTenantListModel tenantsModel = await this.userTenantContainer.GetAllAsync(tenantInput);
            if (tenantsModel.Models.Count == 0)
            {
                throw new Exception("Not granted access to that tenant");
            }

            DateTime? expirationTime = null;
            if (this.config.Global.ClientAuth.TokenExpirationDuration != null)
            {
                expirationTime = DateTime.Now.Add(TimeSpan.ParseExact(this.config.Global.ClientAuth.TokenExpirationDuration, "c", null));
            }

            // if successful, then mint token
            var jwtHandler = new JwtSecurityTokenHandler();
            var claims = new List<Claim>();
            claims.Add(new Claim("client_id", input.ClientId));
            claims.Add(new Claim("sub", input.ClientId));
            claims.Add(new Claim("name", input.ClientId));
            claims.Add(new Claim("type", "Client Credentials"));

            string tokenString = jwtHandler.WriteToken(await this.jwtHelper.GetIdentityToken(claims, input.Scope, "IoTPlatform", expirationTime));

            return this.StatusCode(200, tokenString);
        }

        [HttpGet]
        [Route("connect/logout")]
        public IActionResult Get([FromQuery] string post_logout_redirect_uri)
        {
            // Validate Input
            if (!Uri.IsWellFormedUriString(post_logout_redirect_uri, UriKind.Absolute))
            {
                throw new Exception("Redirect Uri is not valid!");
            }

            var uri = new UriBuilder(post_logout_redirect_uri);

            return this.Redirect(
                uri.Uri.ToString());
        }

        [HttpPost("connect/switch/{tenant}")]
        public async Task<ActionResult> PostAsync([FromHeader(Name = "Authorization")] string authHeader, [FromRoute] string tenant)
        {
            if (authHeader == null || !authHeader.StartsWith("Bearer"))
            {
                throw new NoAuthorizationException("No Bearer Token Authorization Header was passed.");
            }

            // Extract Bearer token
            string encodedToken = authHeader.Substring("Bearer ".Length).Trim();
            var jwtHandler = new JwtSecurityTokenHandler();
            if (!this.jwtHelper.TryValidateToken("IoTPlatform", encodedToken, this.HttpContext, out JwtSecurityToken jwt))
            {
                throw new NoAuthorizationException("The given token could not be read or validated.");
            }

            if (jwt?.Claims?.Count(c => c.Type == "sub") == 0)
            {
                throw new NoAuthorizationException("Not allowed access. No User Claims");
            }

            // Create a userTenantInput for the purpose of finding if the user has access to the space
            UserTenantInput tenantInput = new UserTenantInput
            {
                UserId = jwt?.Claims?.Where(c => c.Type == "sub").First()?.Value,
                Tenant = tenant,
            };
            UserTenantModel tenantResult = await this.userTenantContainer.GetAsync(tenantInput);
            if (tenantResult != null)
            {
                // Everything checks out so you can mint a new token
                var tokenString = jwtHandler.WriteToken(await this.jwtHelper.GetIdentityToken(jwt.Claims.Where(c => new List<string>() { "sub", "name", "email" }.Contains(c.Type)).ToList(), tenant, jwt.Audiences.First(), jwt.ValidTo));

                return this.StatusCode(200, tokenString);
            }
            else
            {
                throw new NoAuthorizationException("Not allowed access to this tenant.");
            }
        }

        [HttpPost("connect/callback")]
        public async Task<object> PostAsync(
            [FromForm] string state,
            [FromForm] string id_token,
            [FromForm] string error,
            [FromForm] string error_description)
        {
            if (!string.IsNullOrEmpty(error))
            {
                // If there was an error returned from B2C, throw it as an expcetion
                throw new Exception($"Azure B2C returned an error: {{{error}: {error_description}}}");
            }

            AuthState authState = null;
            try
            {
                authState = JsonConvert.DeserializeObject<AuthState>(state);
            }
            catch (Exception e)
            {
                throw new Exception("Invlid state from authentication redirect", e);
            }

            var originalAudience = authState.ClientId;

            // Bring over Subject and Name
            var jwtHandler = new JwtSecurityTokenHandler();
            var jwt = jwtHandler.ReadJwtToken(id_token);
            var claims = jwt.Claims.Where(t => new List<string> { "sub", "name" }.Contains(t.Type)).ToList();
            string invitedTenant = authState.Tenant;
            string userNameOrEmail = string.Empty;

            // If theres an invitation token then add user to tenant
            if (!string.IsNullOrEmpty(authState.Invitation))
            {
                var inviteJWT = jwtHandler.ReadJwtToken(authState.Invitation);
                string inviteUserId = inviteJWT.Claims.Where(c => c.Type == "userId").First().Value;
                string newUserId = claims.Where(c => c.Type == "sub").First().Value;
                invitedTenant = inviteJWT.Claims.Where(c => c.Type == "tenant").First().Value;
                string invitedBy = inviteJWT.Claims.Where(c => c.Type == "invitedby").First().Value;

                // Extract first email
                var emailClaim = jwt.Claims.Where(t => t.Type == "emails").FirstOrDefault();
                if (emailClaim != null)
                {
                    claims.Add(new Claim("email", emailClaim.Value));
                    var userName = claims.Where(c => c.Type == "name").FirstOrDefault();
                    if (userName == null)
                    {
                        // Adding the name to claims as the update name code block fails inside method GetIdentityToken in JwtHelpers
                        claims.Add(new Claim("name", emailClaim.Value));
                        userNameOrEmail = emailClaim.Value;
                    }
                    else
                    {
                        userNameOrEmail = userName.Value;
                    }
                }

                UserTenantInput userTenant = new UserTenantInput()
                {
                    UserId = newUserId,
                    Tenant = invitedTenant,
                    Roles = JsonConvert.SerializeObject(inviteJWT.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList()),
                    Type = "Member",
                    Name = userNameOrEmail,
                    CreatedBy = invitedBy,
                    CreatedTime = DateTime.UtcNow,
                };
                await this.userTenantContainer.UpdateAsync(userTenant);

                UserSettingsInput userSettings = new UserSettingsInput()
                {
                    UserId = newUserId,
                    SettingKey = "inviteId",
                    Value = inviteUserId,
                };

                // Store invite Id
                await this.userSettingsContainer.UpdateAsync(userSettings);

                // Transfer settings to new userID
                await this.userSettingsContainer.UpdateUserIdAsync(inviteUserId, newUserId);

                try
                {
                    // Delete placeholder for invite
                    userTenant.UserId = inviteUserId;
                    await this.userTenantContainer.DeleteAsync(userTenant);
                }
                catch (Exception)
                {
                    // Do nothing, delete will fail only for scenarios where the update is incorrectly working or if the invited user gets deleted for some reason.
                    // Not throwing an exception because a delete failure will not break the code however there will be some unused invites
                }
            }

            if (!string.IsNullOrEmpty(authState.Nonce))
            {
                claims.Add(new Claim("nonce", authState.Nonce));
            }

            DateTime? expirationTime = null;
            if (this.config.Global.ClientAuth.TokenExpirationDuration != null)
            {
                expirationTime = DateTime.Now.Add(TimeSpan.ParseExact(this.config.Global.ClientAuth.TokenExpirationDuration, "c", null));
            }

            string accessTokenString = jwtHandler.WriteToken(await this.jwtHelper.GetIdentityToken(claims.Select(t => (Claim)t.Clone()).ToList(), invitedTenant, originalAudience, expirationTime));
            claims.Add(new Claim("at_hash", this.jwtHelper.AtHash(accessTokenString)));
            string idTokenString =
                jwtHandler.WriteToken(
                    await this.jwtHelper.GetIdentityToken(claims, invitedTenant, originalAudience, expirationTime));

            // Build Return Uri
            var returnUri = new UriBuilder(authState.ReturnUrl);

            // Need to build Query carefully to not clobber other query items -- just injecting state
            // var query = HttpUtility.ParseQueryString(returnUri.Query);
            // query["state"] = HttpUtility.UrlEncode(authState.state);
            // returnUri.Query = query.ToString();
            returnUri.Fragment =
                "id_token=" + idTokenString + "&state=" +
                HttpUtility.UrlEncode(authState
                    .State) // pass token in Fragment for more security (Browser wont forward...)
                + "&access_token=" + accessTokenString;
            return this.Redirect(returnUri.Uri.ToString());
        }
    }
}