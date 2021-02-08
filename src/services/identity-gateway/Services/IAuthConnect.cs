// <copyright file="IAuthConnect.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Mmm.Iot.IdentityGateway.Services.Models;

namespace Mmm.Iot.IdentityGateway.Services
{
    public interface IAuthConnect
    {
        public UriBuilder GetLoginRedirectUri();

        public UriBuilder GetLogoutRedirectUri(string post_logout_redirect_uri);

        public Task<bool> AcquireTokenAndValidate(ClientCredentialInput input);

        public List<Claim> GetClaims(JwtSecurityToken jwt);
    }
}