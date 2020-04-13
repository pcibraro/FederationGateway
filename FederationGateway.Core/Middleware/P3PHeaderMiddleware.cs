using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FederationGateway.Core.Middleware
{
    public class P3PHeaderMiddleware
    {
        private readonly RequestDelegate _next;

        public P3PHeaderMiddleware(RequestDelegate next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));

            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.Headers.Add("P3P", "CP=\"NID DSP ALL COR\"");

            await _next(context);

            
        }
    }
}

