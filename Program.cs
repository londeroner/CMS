using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using System.Diagnostics;
using LoggingService;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Sinks.SystemConsole.Themes;
using DataService;
using FunctionalService;

namespace CMS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    var DPcontext = services.GetRequiredService<DataProtectionKeysContext>();
                    var functional = services.GetRequiredService<IFunctionalSvc>();

                    DbContextInitializer.Initialize(DPcontext, context, functional).Wait();
                }
                catch (Exception e)
                {
                    Log.Error("{Message}", e.Message);
                }
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).
            UseSerilog((hostingContext, loggingConfiguration) => loggingConfiguration
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", "CMS")
                    .Enrich.WithProperty("MachineName", Environment.MachineName)
                    .Enrich.WithProperty("CurrentManagedThreadId", Environment.CurrentManagedThreadId)
                    .Enrich.WithProperty("OSVersion", Environment.OSVersion)
                    .Enrich.WithProperty("Version", Environment.Version)
                    .Enrich.WithProperty("UserName", Environment.UserName)
                    .Enrich.WithProperty("ProcessId", Process.GetCurrentProcess().Id)
                    .Enrich.WithProperty("Processname", Process.GetCurrentProcess().ProcessName)
                    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                    .WriteTo.File(new CustomTextFormatter(), Path.Combine(hostingContext.HostingEnvironment.ContentRootPath +
                        $"{Path.DirectorySeparatorChar}Logs{Path.DirectorySeparatorChar}", $"load_error_{DateTime.Now:yyyyMMdd}.txt"))
                    .ReadFrom.Configuration(hostingContext.Configuration))
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
