// <copyright file="ExternalRequestHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.Http;
using Mmm.Iot.Common.Services.Models;
using Newtonsoft.Json;
using HttpRequest = Mmm.Iot.Common.Services.Http.HttpRequest;

namespace Mmm.Iot.Common.Services.Helpers
{
    public class ExternalRequestHelper : IExternalRequestHelper
    {
        private const string TenantHeader = "ApplicationTenantID";
        private const string AzdsRouteKey = "azds-route-as";
        private const string CTokenHeader = "x-ms-continuation";

        private readonly IHttpClient httpClient;
        private readonly IHttpContextAccessor httpContextAccessor;

        public ExternalRequestHelper(IHttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            this.httpClient = httpClient;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<StatusServiceModel> ProcessStatusAsync(string uri)
        {
            return await this.ProcessRequestAsync<StatusServiceModel>(HttpMethod.Get, $"{uri}/status");
        }

        public async Task<T> ProcessRequestAsync<T>(HttpMethod method, string url, string tenantId = null, NameValueCollection headers = null)
        {
            IHttpRequest request = this.CreateRequest(url, tenantId, headers);
            return await this.SendRequestAsync<T>(method, request);
        }

        public async Task<T> ProcessRequestAsync<T>(HttpMethod method, string url, T content, string tenantId = null, NameValueCollection headers = null)
        {
            IHttpRequest request = this.CreateRequest(url, content, tenantId);
            return await this.SendRequestAsync<T>(method, request);
        }

        public async Task<IHttpResponse> ProcessRequestAsync(HttpMethod method, string url, string tenantId = null, NameValueCollection headers = null)
        {
            IHttpRequest request = this.CreateRequest(url, tenantId, headers);
            return await this.SendRequestAsync(method, request);
        }

        public async Task<T> SendRequestAsync<T>(HttpMethod method, IHttpRequest request)
        {
            IHttpResponse response = await this.SendRequestAsync(method, request);
            string responseContent = response?.Content?.ToString();
            try
            {
                return JsonConvert.DeserializeObject<T>(responseContent);
            }
            catch (Exception e)
            {
                throw new JsonReaderException("Unable to deserialize response content to the proper API model.", e);
            }
        }

        public async Task<IHttpResponse> SendRequestAsync(HttpMethod method, IHttpRequest request)
        {
            IHttpResponse response = null;
            try
            {
                response = await this.httpClient.SendAsync(request, method);
            }
            catch (Exception e)
            {
                throw new HttpRequestException("An error occurred while sending the request.", e);
            }

            this.ThrowIfError(response, request);
            return response;
        }

        private IHttpRequest CreateRequest(string url, string tenantId = null, NameValueCollection headers = null)
        {
            var request = new HttpRequest();
            request.SetUriFromString(url);

            if (string.IsNullOrEmpty(tenantId))
            {
                try
                {
                    tenantId = this.httpContextAccessor.HttpContext.Request.GetTenant();
                }
                catch (Exception e)
                {
                    throw new ArgumentException("The tenantId for the External Request was not provided and could not be retrieved from the HttpContextAccessor Request.", e);
                }
            }

            request.AddHeader(TenantHeader, tenantId);

            if (headers != null)
            {
                foreach (string item in headers.AllKeys)
                {
                    request.AddHeader(item, headers[item]);
                }
            }

            if (url.ToLowerInvariant().StartsWith("https:"))
            {
                request.Options.AllowInsecureSSLServer = true;
            }

            if (this.httpContextAccessor.HttpContext != null && this.httpContextAccessor.HttpContext.Request.Headers.ContainsKey(AzdsRouteKey))
            {
                try
                {
                    var azdsRouteAs = this.httpContextAccessor.HttpContext.Request.Headers.First(p => string.Equals(p.Key, AzdsRouteKey, StringComparison.OrdinalIgnoreCase));
                    request.Headers.Add(AzdsRouteKey, azdsRouteAs.Value.First());  // azdsRouteAs.Value returns an iterable of strings, take the first
                }
                catch (Exception e)
                {
                    throw new Exception($"Unable to attach the {AzdsRouteKey} header to the IdentityGatewayClient Request.", e);
                }
            }

            return request;
        }

        private IHttpRequest CreateRequest<T>(string url, T content, string tenantId, NameValueCollection headers = null)
        {
            IHttpRequest request = this.CreateRequest(url, tenantId, headers);
            request.SetContent(content);
            return request;
        }

        private void ThrowIfError(IHttpResponse response, IHttpRequest request)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    throw new ResourceNotFoundException(response.Content);

                case HttpStatusCode.Conflict:
                    throw new ConflictingResourceException(response.Content);

                default:
                    throw new HttpRequestException(response.Content);
            }
        }
    }
}