using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FederationGateway.Core.Middleware
{
    public class P3PHeaderMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<P3PHeaderMiddleware> _logger;

        public P3PHeaderMiddleware(RequestDelegate next, ILogger<P3PHeaderMiddleware> logger)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));

            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogInformation("Adding P3P header for cookies");

            context.Response.Headers.Add("P3P", "CP=\"NID DSP ALL COR\"");

            await _next(context);

            
        }
    }
}

