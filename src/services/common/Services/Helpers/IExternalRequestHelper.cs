// <copyright file="IExternalRequestHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Specialized;
using System.Net.Http;
using System.Threading.Tasks;
using Mmm.Iot.Common.Services.Http;
using Mmm.Iot.Common.Services.Models;

namespace Mmm.Iot.Common.Services.Helpers
{
    public interface IExternalRequestHelper
    {
        Task<StatusServiceModel> ProcessStatusAsync(string serviceUri);

        Task<T> ProcessRequestAsync<T>(HttpMethod method, string url, string tenantId = null, NameValueCollection headers = null);

        Task<T> ProcessRequestAsync<T>(HttpMethod method, string url, T content, string tenantId = null, NameValueCollection headers = null);

        Task<IHttpResponse> ProcessRequestAsync(HttpMethod method, string url, string tenantId = null, NameValueCollection headers = null);

        Task<T> SendRequestAsync<T>(HttpMethod method, IHttpRequest request);

        Task<IHttpResponse> SendRequestAsync(HttpMethod method, IHttpRequest request);
    }
}