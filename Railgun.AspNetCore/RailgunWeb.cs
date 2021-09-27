using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Railgun.Api;

namespace Railgun.AspNetCore
{
    public enum EndpointTypes
    {
        Get,
        Post
    }
    
    public class RailgunWeb
    {
        public void RouteGet(string pattern, IRailgunClosure fn)
        {
        }
        
        public void Start(IRailgunClosure fn)
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
                            endpoints.MapGet("/{name}", async context =>
                            {
                                Console.WriteLine(context.GetRouteData().Values["name"]);
                                var res = (string) fn.Eval(Seq.Create(new[] {context}));
                                await context.Response.WriteAsync(res);
                            });
                        });
                    });
                });
            h.Build().Start();
        }
    }
}