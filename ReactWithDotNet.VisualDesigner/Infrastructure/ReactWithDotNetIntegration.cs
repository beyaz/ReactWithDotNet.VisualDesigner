using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using ReactWithDotNet.UIDesigner;

namespace ReactWithDotNet.VisualDesigner.Infrastructure;

public static class ReactWithDotNetIntegration
{
    public static void ConfigureReactWithDotNet(this WebApplication app)
    {
        app.UseMiddleware<ReactWithDotNetJavaScriptFiles>();

        var routeMap = RouteHelper.GetRoutesFrom(Plugin.Plugins);

        RequestHandlerPath = "/" + nameof(HandleReactWithDotNetRequest);

        app.Use(async (httpContext, next) =>
        {
            var path = httpContext.Request.Path.Value ?? string.Empty;

            if (path == RequestHandlerPath)
            {
                await HandleReactWithDotNetRequest(httpContext);
                return;
            }

            if (routeMap.TryGetValue(path, out var routeInfo))
            {
                await WriteHtmlResponse(httpContext, typeof(MainLayout), routeInfo.Page);
                return;
            }

            #if DEBUG
            if (path == ReactWithDotNetDesigner.UrlPath)
            {
                await WriteHtmlResponse(httpContext, typeof(MainLayout), typeof(ReactWithDotNetDesigner));
                return;
            }
            #endif

            await next();
        });

        app.Use(async (httpContext, next) =>
        {
            var path = httpContext.Request.Path.Value ?? string.Empty;

            foreach (var localFilePath in await TryFindFilePathFromWebRequestPath(path))
            {
                foreach (var (contentType, fileBytes) in await TryConvertLocalFilePathToFileContentResultData(localFilePath))
                {
                    await Results.File(fileBytes, contentType).ExecuteAsync(httpContext);

                    return;
                }
            }

            await next();
        });

        app.MapPost("/shutdown", async _ =>
        {
            if (!Debugger.IsAttached)
            {
                await app.StopAsync();  
            }
        });
    }

    static Task HandleReactWithDotNetRequest(HttpContext httpContext)
    {
        httpContext.Response.ContentType = "application/json; charset=utf-8";

        return ProcessReactWithDotNetComponentRequest(new()
        {
            HttpContext           = httpContext,
            OnReactContextCreated = OnReactContextCreated
        });
    }

    static Task OnReactContextCreated(ReactContext context)
    {
        return Task.CompletedTask;
    }

    static Task WriteHtmlResponse(HttpContext httpContext, Type layoutType, Type mainContentType)
    {
        Cache.Clear();

        httpContext.Response.ContentType = "text/html; charset=UTF-8";

        httpContext.Response.Headers[HeaderNames.CacheControl] = "no-cache, no-store, must-revalidate";
        httpContext.Response.Headers[HeaderNames.Expires]      = "0";
        httpContext.Response.Headers[HeaderNames.Pragma]       = "no-cache";

        return ProcessReactWithDotNetPageRequest(new()
        {
            LayoutType            = layoutType,
            MainContentType       = mainContentType,
            HttpContext           = httpContext,
            OnReactContextCreated = OnReactContextCreated
        });
    }
}