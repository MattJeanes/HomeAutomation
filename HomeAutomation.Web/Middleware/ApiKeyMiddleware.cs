using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Threading.Tasks;

namespace HomeAutomation.Web.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly string _apiKey;
        private readonly RequestDelegate _next;

        public ApiKeyMiddleware(RequestDelegate next, string apiKey)
        {
            _apiKey = apiKey;
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments(new PathString("/api")) && !CheckApiKey(context.Request))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }
            await _next.Invoke(context);
        }

        private bool CheckApiKey(HttpRequest request)
        {
            if (!request.Query.ContainsKey("key")) { return false; }
            return request.Query["key"] == _apiKey;
        }
    }

    public static class ApiKeyMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiKeyMiddleware(this IApplicationBuilder app, string apiKey)
        {
            return app.UseMiddleware<ApiKeyMiddleware>(apiKey);
        }
    }
}
