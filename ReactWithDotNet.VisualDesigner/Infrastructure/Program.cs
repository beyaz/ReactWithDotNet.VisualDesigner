using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ReactWithDotNet.VisualDesigner.Infrastructure;

public class Program
{
    public static void Main(string[] args)
    {
        ProcessHelper.KillAllNamedProcess($"{nameof(ReactWithDotNet)}.{nameof(VisualDesigner)}");

        var port = NetworkHelper.GetAvailablePort(Config.NextAvailablePortFrom);

        if (Config.HideConsoleWindow)
        {
            IgnoreException(ConsoleWindowUtility.HideConsoleWindow);
        }

        if (Config.UseUrls)
        {
            var browserApplicationPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Config.BrowserExePathForWindows
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? Config.BrowserExePathForMac
                : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? Config.BrowserExePathForLinux
                : throw new NotSupportedException("Unsupported OS");

            Process.Start(browserApplicationPath, Config.BrowserExeArguments.Replace("{Port}", port.ToString()));
        }

        var builder = WebApplication.CreateBuilder(args);

        var services = builder.Services;

        // C O N F I G U R E     S E R V I C E S
        services.Configure<BrotliCompressionProviderOptions>(options => { options.Level = CompressionLevel.Optimal; });
        services.Configure<GzipCompressionProviderOptions>(options => { options.Level   = CompressionLevel.Optimal; });
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.AddHostedService<ApplicationStateSaveService>();

        // C O N F I G U R E     A P P L I C A T I O N
        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = new("/wwwroot"),

            OnPrepareResponse = ctx =>
            {
                var maxAge = TimeSpan.FromMinutes(5).TotalSeconds;

                ctx.Context.Response.Headers.Append("Cache-Control", $"public, max-age={maxAge}");
            }
        });

        app.UseResponseCompression();

        app.ConfigureReactWithDotNet();

        if (Config.UseUrls)
        {
            app.Run($"https://*:{port}");
        }
        else
        {
            app.Run();
        }
    }
}