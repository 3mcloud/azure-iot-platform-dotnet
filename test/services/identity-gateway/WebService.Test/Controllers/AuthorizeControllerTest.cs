// <copyright file="AuthorizeControllerTest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.TestHelpers;
using Mmm.Iot.IdentityGateway.Controllers;
using Mmm.Iot.IdentityGateway.Services;
using Mmm.Iot.IdentityGateway.Services.Helpers;
using Mmm.Iot.IdentityGateway.Services.Models;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Mmm.Iot.IdentityGateway.WebService.Test.Controllers
{
    public class AuthorizeControllerTest : IDisposable
    {
        public static readonly string ValidAuthHeader = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        private const string SomeIssuer = "http://someIssuer";
        private const string SomePublicKey = "-----BEGIN PUBLIC KEY-----\r\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAryQICCl6NZ5gDKrnSztO\r\n3Hy8PEUcuyvg/ikC+VcIo2SFFSf18a3IMYldIugqqqZCs4/4uVW3sbdLs/6PfgdX\r\n7O9D22ZiFWHPYA2k2N744MNiCD1UE+tJyllUhSblK48bn+v1oZHCM0nYQ2NqUkvS\r\nj+hwUU3RiWl7x3D2s9wSdNt7XUtW05a/FXehsPSiJfKvHJJnGOX0BgTvkLnkAOTd\r\nOrUZ/wK69Dzu4IvrN4vs9Nes8vbwPa/ddZEzGR0cQMt0JBkhk9kU/qwqUseP1QRJ\r\n5I1jR4g8aYPL/ke9K35PxZWuDp3U0UPAZ3PjFAh+5T+fc7gzCs9dPzSHloruU+gl\r\nFQIDAQAB\r\n-----END PUBLIC KEY-----";
        private const string SomeUri = "http://azureb2caseuri.com";
        private Mock<ILogger<UserTenantContainer>> logger;
        private string invite = "someInvite";
        private bool disposedValue = false;
        private Mock<IUserContainer<UserSettingsModel, UserSettingsInput>> mockUserSettingsContainer;
        private Mock<UserTenantContainer> mockUserTenantContainer;
        private AuthorizeController controller;
        private string someUiRedirectUri = new Uri("http://valid-uri.com").AbsoluteUri;
        private Guid someTenant = Guid.NewGuid();
        private Mock<HttpContext> mockHttpContext;
        private string state = "someState";
        private string clientId = "someClientId";
        private string nonce = "someNonce";
        private Mock<AppConfig> mockAppConfig;
        private Mock<IJwtHelpers> mockJwtHelper;
        private Mock<IAuthenticationContext> mockAuthContext;
        private JwtSecurityToken someSecurityToken;
        private Mock<IOpenIdProviderConfiguration> mockOpenIdProviderConfiguration;

        public AuthorizeControllerTest()
        {
            this.InitializeController();
            this.SetupDefaultBehaviors();
        }

        public static IEnumerable<object[]> GetJwtSecurityTokens()
        {
            yield return new object[] { null };
            yield return new object[] { new JwtSecurityToken(null, null, new List<Claim> { new Claim("available_tenants", Guid.NewGuid().ToString()) }) };
        }

        public static IEnumerable<object[]> GetInvalidAuthHeaders()
        {
            yield return new object[] { "Bearer not-a-valid-auth-header" };
            yield return new object[] { "Bearer " };
            yield return new object[] { ValidAuthHeader };
        }

        [Theory]
        [Trait(Constants.Type, Constants.UnitTest)]
        [InlineData("not-a-valid-uri")]
        [InlineData(null)]
        [InlineData("")]
        public void AuthorizeThrowsWhenRedirectUriNotValid(string invalidUri)
        {
            // Arrange
            // Act
            Action a = () => this.controller.Get(invalidUri, null, null, null, null, null);

            // Assert
            Assert.Throws<Exception>(a);
        }

        [Theory]
        [Trait(Constants.Type, Constants.UnitTest)]
        [InlineData("x")]
        [InlineData("7")]
        [InlineData("")]
        [InlineData("not-a-valid-guid")]
        public void AuthorizeThrowsWhenTenantNotValid(string invalidTenant)
        {
            // Arrange
            // Act
            Action a = () => this.controller.Get(this.someUiRedirectUri, null, null, null, invalidTenant, null);

            // Assert
            Assert.Throws<Exception>(a);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public void AuthorizeRedirectsToRedirectUri()
        {
            // Arrange
            // Act
            var redirectResult = this.controller.Get(this.someUiRedirectUri, this.state, this.clientId, this.nonce, this.someTenant.ToString(), this.invite) as RedirectResult;

            // Assert
            Assert.NotNull(redirectResult);
            var uriResult = new Uri(redirectResult.Url);
            Assert.NotNull(uriResult.Query);
            Assert.NotEmpty(uriResult.Query);
            var queryStrings = HttpUtility.ParseQueryString(uriResult.Query);
            Assert.Contains("state", queryStrings.AllKeys);
            var returnedState = JObject.Parse(queryStrings["state"]);
            Assert.Equal(this.someUiRedirectUri, returnedState["returnUrl"]);
            Assert.Equal(this.state, returnedState["state"]);
            Assert.Equal(this.someTenant, returnedState["tenant"]);
            Assert.Equal(this.nonce, returnedState["nonce"]);
            Assert.Equal(this.clientId, returnedState["client_id"]);
            Assert.Equal($"{SomeIssuer}/connect/callback", queryStrings["redirect_uri"]);
            Assert.Equal(this.invite, returnedState["invitation"]);
        }

        [Theory]
        [Trait(Constants.Type, Constants.UnitTest)]
        [InlineData("not-a-valid-uri")]
        [InlineData(null)]
        [InlineData("")]
        public void LogoutThrowsWhenRedirectUriNotValid(string invalidUri)
        {
            // Arrange
            // Act
            Action a = () => this.controller.Get(invalidUri);

            // Assert
            Assert.Throws<Exception>(a);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public void LogoutRedirectsToRedirectUri()
        {
            // Arrange
            // Act
            var redirectResult = this.controller.Get(this.someUiRedirectUri) as RedirectResult;

            // Assert
            Assert.Equal(this.someUiRedirectUri, redirectResult.Url);
        }

        [Theory]
        [Trait(Constants.Type, Constants.UnitTest)]
        [InlineData("not-a-valid-auth-header")]
        [InlineData(null)]
        [InlineData("")]
        public async Task SwitchTenantThrowsWhenAuthorizationHeaderNotValid(string invalidAuthHeader)
        {
            // Arrange
            // Act
            Func<Task> a = async () => await this.controller.PostAsync(invalidAuthHeader, null);

            // Assert
            await Assert.ThrowsAsync<NoAuthorizationException>(a);
        }

        [Theory]
        [Trait(Constants.Type, Constants.UnitTest)]
        [MemberData(nameof(GetInvalidAuthHeaders))]
        public async Task SwitchTenantThrowsWhenAuthorizationHeaderTokenNotReadableOrValid(string invalidAuthHeader)
        {
            // Arrange
            JwtSecurityToken jwtSecurityToken = null;
            this.mockJwtHelper.Setup(m => m.TryValidateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpContext>(), out jwtSecurityToken)).Returns(false);

            // Act
            Func<Task> a = async () => await this.controller.PostAsync(invalidAuthHeader, null);

            // Assert
            await Assert.ThrowsAsync<NoAuthorizationException>(a);
        }

        [Theory]
        [Trait(Constants.Type, Constants.UnitTest)]
        [MemberData(nameof(GetJwtSecurityTokens))]
#pragma warning disable xUnit1026
        public async Task SwitchTenantThrowsWhenTenantAccessNotAllowed(JwtSecurityToken jwtSecurityToken)
#pragma warning restore xUnit1026
        {
            // Arrange
            this.mockJwtHelper.Setup(m => m.TryValidateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpContext>(), out jwtSecurityToken)).Returns(true);

            // Act
            Func<Task> a = async () => await this.controller.PostAsync(ValidAuthHeader, this.someTenant.ToString());

            // Assert
            await Assert.ThrowsAsync<NoAuthorizationException>(a);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task SwitchTenantMintsNewTokenWithNewTenant()
        {
            // Arrange
            var currentTenant = this.someTenant;
            var availableTenant = Guid.NewGuid();
            var claimWithCurrentTenant = new Claim("available_tenants", currentTenant.ToString());
            var claimWithAvailableTenant = new Claim("available_tenants", availableTenant.ToString());
            var subClaim = new Claim("sub", "someSub");
            var audClaim = new Claim("aud", "someAud");
            var expClaim = new Claim("exp", "1571240635");
            var jwtSecurityToken = new JwtSecurityToken(null, null, new List<Claim> { claimWithCurrentTenant, claimWithAvailableTenant, subClaim, audClaim, expClaim });

            // return sucessfully a UserTenant
            this.mockUserTenantContainer.Setup(s => s.GetAsync(It.IsAny<UserTenantInput>())).ReturnsAsync(new UserTenantModel("test", "test"));

            this.mockJwtHelper.Setup(m => m.TryValidateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpContext>(), out jwtSecurityToken)).Returns(true);
            this.mockJwtHelper.Setup(m => m.GetIdentityToken(It.IsAny<List<Claim>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>())).ReturnsAsync(jwtSecurityToken);

            // Act
            var objectResult = await this.controller.PostAsync(ValidAuthHeader, availableTenant.ToString()) as ObjectResult;

            // Assert
            Assert.Equal(StatusCodes.Status200OK, objectResult.StatusCode);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
#pragma warning disable xUnit1026
        public async Task ErrorInClientCredentialsAuthentication()
#pragma warning restore xUnit1026
        {
            // Arrange
            this.mockAuthContext.Setup(m => m.AcquireTokenAsync(It.IsAny<string>(), It.IsAny<ClientCredential>())).Throws(new Exception("Authentication Failed!"));

            // Act
            var result = await this.controller.PostTokenAsync(new ClientCredentialInput { ClientId = Guid.NewGuid().ToString(), ClientSecret = "djdhafkjda6Z0TWSm6lyPHKsx7H*F" }) as ObjectResult;

            // Assert
            Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task SuccessClientCredentialsAuthentication()
        {
            // Arrange
            this.mockAuthContext.Setup(m => m.AcquireTokenAsync(It.IsAny<string>(), It.IsAny<ClientCredential>())).Returns(Task.FromResult<AuthenticationResult>(null));
            this.mockJwtHelper.Setup(m => m.GetIdentityToken(It.IsAny<List<Claim>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>())).ReturnsAsync(new JwtSecurityToken("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWV9.TJVA95OrM7E2cBab30RMHrHDcEfxjoYZgeFONFh7HgQ"));
            var tenantId = Guid.NewGuid().ToString();
            this.mockUserTenantContainer.Setup(s => s.GetAllAsync(It.IsAny<UserTenantInput>())).ReturnsAsync(new UserTenantListModel(new List<UserTenantModel> { new UserTenantModel("test", tenantId) }));

            // Act
            var result = await this.controller.PostTokenAsync(new ClientCredentialInput { ClientId = Guid.NewGuid().ToString(), ClientSecret = "djdhafkjda6Z0TWSm6lyPHKsx7H*F", Scope = tenantId }) as ObjectResult;

            // Assert
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.IsType<string>(result.Value);
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.controller.Dispose();
                }

                this.disposedValue = true;
            }
        }

        private void InitializeController()
        {
            this.mockAppConfig = new Mock<AppConfig> { DefaultValue = DefaultValue.Mock };
            this.logger = new Mock<ILogger<UserTenantContainer>>();
            this.mockUserTenantContainer = new Mock<UserTenantContainer>(this.logger.Object);
            this.mockUserSettingsContainer = new Mock<IUserContainer<UserSettingsModel, UserSettingsInput>>();
            this.mockJwtHelper = new Mock<IJwtHelpers> { DefaultValue = DefaultValue.Mock };
            this.mockAuthContext = new Mock<IAuthenticationContext> { DefaultValue = DefaultValue.Mock };
            this.mockHttpContext = new Mock<HttpContext> { DefaultValue = DefaultValue.Mock };
            this.mockOpenIdProviderConfiguration = new Mock<IOpenIdProviderConfiguration> { DefaultValue = DefaultValue.Mock };
            this.controller = new AuthorizeController(this.mockAppConfig.Object, this.mockUserTenantContainer.Object, this.mockUserSettingsContainer.Object as UserSettingsContainer, this.mockJwtHelper.Object, this.mockOpenIdProviderConfiguration.Object, this.mockAuthContext.Object, null)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = this.mockHttpContext.Object,
                },
            };
        }

        private void SetupDefaultBehaviors()
        {
            this.mockAppConfig.Setup(m => m.Global.AzureB2cBaseUri).Returns(SomeUri);
            this.mockAppConfig.Setup(m => m.IdentityGatewayService.PublicKey).Returns(SomePublicKey);
            this.someSecurityToken = new JwtSecurityToken(null, null, new List<Claim> { new Claim("available_tenants", this.someTenant.ToString()) });
            this.mockJwtHelper.Setup(m => m.TryValidateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpContext>(), out this.someSecurityToken)).Returns(true);
            this.mockOpenIdProviderConfiguration.Setup(m => m.Issuer).Returns(SomeIssuer);
        }
    }
}