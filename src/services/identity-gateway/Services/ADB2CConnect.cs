// <copyright file="ADB2CConnect.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.IdentityGateway.Services.Models;

namespace Mmm.Iot.IdentityGateway.Services
{
    public class ADB2CConnect : IADB2CConnect
    {
        private AppConfig config;
        private IAuthenticationContext authenticationContext;

        public ADB2CConnect(AppConfig config, IAuthenticationContext authenticationContext)
        {
            this.config = config;
            this.authenticationContext = authenticationContext;
        }

        public async Task<bool> AcquireTokenAndValidate(ClientCredentialInput input)
        {
            string resourceUri = "https://graph.microsoft.com/";
            ClientCredential clientCredential = new ClientCredential(input.ClientId, input.ClientSecret);
            try
            {
                AuthenticationResult token = await this.authenticationContext.AcquireTokenAsync(resourceUri, clientCredential);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public UriBuilder GetLoginRedirectUri()
        {
            var uri = new UriBuilder(this.config.Global.AzureB2cBaseUri);
            return uri;
        }

        public UriBuilder GetLogoutRedirectUri(string post_logout_redirect_uri)
        {
            var uri = new UriBuilder(post_logout_redirect_uri);
            return uri;
        }

        public List<Claim> GetClaims(System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwt)
        {
            List<Claim> claims = new List<Claim>();
            claims = jwt.Claims.Where(t => new List<string> { "name", "sub" }.Contains(t.Type)).ToList();
            return claims;
        }
    }
}