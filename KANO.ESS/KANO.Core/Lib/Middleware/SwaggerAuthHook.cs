using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace KANO.Core.Lib.Middleware
{
    public class SwaggerAuthHookMiddleware : IMiddleware
    {
        // Note: Currently only works for the UI, still finding how to protect the json file
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Path.Value.Contains("/swagger/"))
            {
                var isauth = context.User?.Identity?.IsAuthenticated == true;
                if (!isauth)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await context.Response.WriteAsync("{\"StatusCode\":401,\"Message\":\"Not Authorized\",\"Data\":null}");
                    return;
                }
            }
            await next(context);
        }

    }

    public static class SwaggerAuthHookExtensions
    {
        public static IApplicationBuilder UseSwaggerAuthHook(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SwaggerAuthHookMiddleware>();
        }
    }
}
