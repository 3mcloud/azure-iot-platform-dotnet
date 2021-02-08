// <copyright file="Auth0Connect.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.IdentityGateway.Services.Models;

namespace Mmm.Iot.IdentityGateway.Services
{
    public class Auth0Connect : IAuth0Connect
    {
        private AppConfig config;

        public Auth0Connect(AppConfig config)
        {
            this.config = config;
        }

        public async Task<bool> AcquireTokenAndValidate(ClientCredentialInput input)
        {
            List<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>();
            data.Add(new KeyValuePair<string, string>("audience", this.config.AuthProvider.Auth0.TokenAudience));
            data.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
            data.Add(new KeyValuePair<string, string>("client_id", input.ClientId));
            data.Add(new KeyValuePair<string, string>("client_secret", input.ClientSecret));

            var response = await this.PostFormUrlEncoded<HttpResponseMessage>(this.config.AuthProvider.Auth0.TokenUrl, data);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            return true;
        }

        public UriBuilder GetLoginRedirectUri()
        {
            var uri = new UriBuilder(this.config.AuthProvider.Auth0.LoginUrl);

            return uri;
        }

        public UriBuilder GetLogoutRedirectUri(string post_logout_redirect_uri)
        {
            var logoutUri = new UriBuilder(this.config.AuthProvider.Auth0.LogoutUrl);

            var query = HttpUtility.ParseQueryString(logoutUri.Query);

            query["returnTo"] = post_logout_redirect_uri;
            logoutUri.Query = query.ToString();

            return logoutUri;
        }

        public List<Claim> GetClaims(JwtSecurityToken jwt)
        {
            List<Claim> claims = new List<Claim>();
            claims = jwt.Claims.Where(t => new List<string> { "name" }.Contains(t.Type)).ToList();

            var oidClaim = jwt.Claims.FirstOrDefault(c => c.Type == "https://serenity.test.com/oid");
            if (oidClaim != null)
            {
                claims.Add(new Claim("sub", oidClaim.Value));
            }

            return claims;
        }

        private async Task<HttpResponseMessage> PostFormUrlEncoded<TResult>(string url, IEnumerable<KeyValuePair<string, string>> postData)
        {
            using (var httpClient = new HttpClient())
            {
                using (var content = new FormUrlEncodedContent(postData))
                {
                    content.Headers.Clear();
                    content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                    HttpResponseMessage response = await httpClient.PostAsync(new Uri(url), content);

                    return response;
                }
            }
        }
    }
}