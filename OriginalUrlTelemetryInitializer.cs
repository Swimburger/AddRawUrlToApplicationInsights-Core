using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;

namespace AddRawUrlToApplicationInsights_Core
{
    public class OriginalUrlTelemetryInitializer : ITelemetryInitializer
    {
        readonly IHttpContextAccessor httpContextAccessor;

        public OriginalUrlTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry is RequestTelemetry requestTelemetry)
            {
                var httpContext = httpContextAccessor.HttpContext;
                if (httpContext?.Request == null)
                {
                    return;
                }

                var request = httpContext.Request;
                var httpRequestFeature = httpContext.Features.Get<IHttpRequestFeature>();
                var rawTarget = httpRequestFeature.RawTarget; // RawTarget returns original path and querystring
                requestTelemetry.Properties["OriginalUrl"] = rawTarget;

                // to extract the path and query from the RawTarget, you need to use the Uri class
                // the Uri class requires at least the scheme://host to parse;
                // adding the dummy base uri "http://0.0.0.0" will ensure it parses properly
                var rawUri = new Uri($"http://0.0.0.0{rawTarget}"); 
                var fdnUrl = new UriBuilder(
                    scheme: request.Scheme,
                    host: request.Host.Host,
                    port: request.Host.Port ?? -1, // use -1 in case there's no port specified, the port will be left out when converting `ToString`
                    path: rawUri.AbsolutePath,
                    extraValue: rawUri.Query
                ).ToString();
                requestTelemetry.Properties["OriginalUrlFqdn"] = fdnUrl;
            }
        }
    }
}