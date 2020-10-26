using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ListenLater {
    public class Program {
        public static void Main(string[] args) {
        
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true,true)
                .Build();
            var key = configuration["LogAnalyticsKey"];
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.AzureAnalytics(workspaceId: "84a91ea4-17d4-48be-b6c7-1f85e47c9773",
                        authenticationId: key)
                    .CreateLogger();
                CreateHostBuilderSerilog(args).Build().Run();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                CreateHostBuilder(args).Build().Run();
            }
        }

        public static IHostBuilder CreateHostBuilderSerilog(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services => {
                    // services.AddSingleton<IScopedProcessingService, ScopedProcessingService>();
                    services.AddHostedService<DeQueueHostedService>();
                    services.AddSingleton<IBackgroundTaskQueue, QueueOfYouTubeAccounts>();
                    services.AddHostedService<ScheduledDownloadNewHostedService>();
                })
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .UseSerilog();

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services => {
                    // services.AddSingleton<IScopedProcessingService, ScopedProcessingService>();
                    services.AddHostedService<DeQueueHostedService>();
                    services.AddSingleton<IBackgroundTaskQueue, QueueOfYouTubeAccounts>();
                    services.AddHostedService<ScheduledDownloadNewHostedService>();
                })
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}