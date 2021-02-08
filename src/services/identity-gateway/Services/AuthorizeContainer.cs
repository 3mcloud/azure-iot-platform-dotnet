// <copyright file="AuthorizeContainer.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.External.AppConfiguration;
using Mmm.Iot.IdentityGateway.Services.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.IdentityGateway.Services
{
    public class AuthorizeContainer
    {
        private const string DefaultAuthProvider = "ADB2C";
        private const string AuthProviderKey = "AuthProvider";
        private readonly IOpenIdProviderConfiguration openIdProviderConfiguration;
        private readonly IAppConfigurationClient appConfigurationClient;
        private readonly IServiceProvider serviceProvider;
        private AppConfig config;
        private IAuth0Connect auth0Connect;
        private IADB2CConnect adB2CConnect;

        public AuthorizeContainer(AppConfig config, IOpenIdProviderConfiguration openIdProviderConfiguration, IServiceProvider serviceProvider, IAuth0Connect auth0Connect, IADB2CConnect adB2CConnect, IAppConfigurationClient appConfigurationClient)
        {
            this.config = config;
            this.openIdProviderConfiguration = openIdProviderConfiguration;
            this.appConfigurationClient = appConfigurationClient;
            this.serviceProvider = serviceProvider;
            this.auth0Connect = auth0Connect;
            this.adB2CConnect = adB2CConnect;
        }

        public UriBuilder GetAuthorizationRedirectUrl(
            string redirect_uri,
            string state,
            string clientId,
            string nonce,
            string tenant,
            string invite)
        {
            var uri = this.GetAuthConnect().GetLoginRedirectUri();

            // Need to build Query carefully to not clobber other query items -- just injecting state
            var query = HttpUtility.ParseQueryString(uri.Query);
            query["state"] = JsonConvert.SerializeObject(new AuthState
            { ReturnUrl = redirect_uri, State = state, Tenant = tenant, Nonce = nonce, ClientId = clientId, Invitation = invite });
            query["redirect_uri"] = this.openIdProviderConfiguration.Issuer + "/connect/callback"; // must be https for B2C
            uri.Query = query.ToString();

            return uri;
        }

        public UriBuilder GetLogoutRedirectUrl(string post_logout_redirect_uri)
        {
            // Validate Input
            if (!Uri.IsWellFormedUriString(post_logout_redirect_uri, UriKind.Absolute))
            {
                throw new Exception("Redirect Uri is not valid!");
            }

            var logoutUri = this.GetAuthConnect().GetLogoutRedirectUri(post_logout_redirect_uri);

            return logoutUri;
        }

        public async Task<bool> AcquireTokenAndValidate(ClientCredentialInput input)
        {
            IAuthConnect authConnect = this.GetAuthConnect();
            bool isTokenAcquired = await authConnect.AcquireTokenAndValidate(input);

            if (!isTokenAcquired)
            {
                throw new Exception("UnAuthorized");
            }

            return isTokenAcquired;
        }

        public List<Claim> GetClaims(JwtSecurityToken jwt)
        {
            List<Claim> claims = new List<Claim>();

            claims.AddRange(this.GetAuthConnect().GetClaims(jwt));

            return claims;
        }

        public async Task<bool> UpdateAuthProvider(string authProvider)
        {
            try
            {
                await this.appConfigurationClient.SetValueAsync(AuthProviderKey, authProvider);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private IAuthConnect GetAuthConnect()
        {
            string authProvider = this.GetAuthProvider();
            switch (authProvider)
            {
                case "ADB2C":
                    return this.adB2CConnect;
                case "Auth0":
                    return this.auth0Connect;
                default:
                    return this.adB2CConnect;
            }
        }

        private string GetAuthProvider()
        {
            string authProvider = DefaultAuthProvider;

            try
            {
                authProvider = this.appConfigurationClient.GetValueFromAppConfig(AuthProviderKey);
            }
            catch
            {
            }

            return authProvider;
        }
    }
}