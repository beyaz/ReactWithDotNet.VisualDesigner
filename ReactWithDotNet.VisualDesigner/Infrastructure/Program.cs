using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO.Compression;
namespace ReactWithDotNet.VisualDesigner.Infrastructure;

public class Program
{
    public static void Main(string[] args)
    {
        // i n i t   c o n f i g
        {
            var config = ReadConfig();
        
            if (config.HasError)
            {
                Console.WriteLine(config.Error);
                Console.Read();
                return;
            }
        
            Config = config.Value;
        }

        // A t t a c h   a s s e m b l y   r e s o l v e r   f o r   p l u g i n s
        {
         
        }

        // SyncHelper.From_SQLite_to_SqlServer.Transfer_From_SQLite_to_SqlServer().GetAwaiter().GetResult();
        // SyncHelper.From_SqlServer_to_SQLite.Transfer_From_SqlServer_to_SQLite().GetAwaiter().GetResult();

        ProcessHelper.KillAllNamedProcess($"{nameof(ReactWithDotNet)}.{nameof(VisualDesigner)}");

        var port = NetworkHelper.GetAvailablePort(Config.NextAvailablePortFrom);

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
        
        NodeJsBridge.Register(app);

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
            TryStartBrowser(port);
        }

        if (Config.HideConsoleWindow)
        {
            Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(_ => IgnoreException(ConsoleWindowUtility.HideConsoleWindow));
        }

        if (Config.UseUrls)
        {
            app.Run($"http://*:{port}");
        }
        else
        {
            app.Run();
        }
    }
}