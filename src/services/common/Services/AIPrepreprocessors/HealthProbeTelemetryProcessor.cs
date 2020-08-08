// <copyright file="HealthProbeTelemetryProcessor.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Mmm.Iot.Common.Services.AIPreprocessors
{
    public class HealthProbeTelemetryProcessor : ITelemetryProcessor
    {
        private const string PingUrlPath = "/ping";
        private const string StatusUrlPath = "v1/status";
        private const string StatusPingUrlPath = "/v1/status/ping";
        private const string RequestPathKey = "RequestPath";
        private const string StatusGetPath = "Status/Get";
        private ITelemetryProcessor next;

        public HealthProbeTelemetryProcessor(ITelemetryProcessor next)
        {
            this.next = next;
        }

        public void Process(ITelemetry item)
        {
            if (item is RequestTelemetry)
            {
                var req = item as RequestTelemetry;
                if (req != null && (req.Url.AbsoluteUri.Contains(PingUrlPath, StringComparison.InvariantCultureIgnoreCase) ||
                    req.Url.AbsoluteUri.Contains(StatusUrlPath, StringComparison.InvariantCultureIgnoreCase)))
                {
                    return;
                }
            }
            else if (item is TraceTelemetry trace)
            {
                if (trace.Properties.TryGetValue(RequestPathKey, out var requestPath) &&
                    (requestPath.Contains(StatusPingUrlPath, StringComparison.InvariantCultureIgnoreCase) || requestPath.Contains(StatusUrlPath, StringComparison.InvariantCultureIgnoreCase)))
                {
                    return;
                }
            }
            else if (item is DependencyTelemetry)
            {
                var dependency = item as DependencyTelemetry;
                if (dependency?.Context?.Operation?.Name != null && dependency.Context.Operation.Name.Contains(StatusGetPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    return;
                }
            }

            // Send everything else
            this.next.Process(item);
        }
    }
}