using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Railgun.AspNetCore
{
    public enum EndpointTypes
    {
        Get,
        Post
    }
    
    public class RailgunWeb
    {
        public void RouteGet(string pattern, RequestDelegate fn)
        {
            
        }
        
        public void Start(string name = "Cactus")
        {
            var h = Host.CreateDefaultBuilder()
                .ConfigureLogging(l =>
                {
                    l.SetMinimumLevel(LogLevel.Critical);
                })
                .ConfigureWebHost(w =>
                {
                    w.UseKestrel();
                    w.UseUrls("http://localhost:7000");
                    w.ConfigureServices(s =>
                    {
                        s.AddRouting();
                    });
                    w.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/", async context =>
                            {
                                await context.Response.WriteAsync($"Hello, {name}!");
                            });
                        });
                    });
                });
            h.Build().Start();
        }
    }
}