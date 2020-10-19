using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ListenLater
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.AzureAnalytics(workspaceId: "84a91ea4-17d4-48be-b6c7-1f85e47c9773", 
                    authenticationId: "BBQKOoUudBchSxrlD2Z+UJkwW149XA8Q57XJML9GPM/Vo9ysyEXk3T166omMw4Krss9YceOjeqphYZVDSCXJZg==")
                .CreateLogger();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    // services.AddSingleton<IScopedProcessingService, ScopedProcessingService>();
                    services.AddHostedService<DeQueueHostedService>();
                    services.AddSingleton<IBackgroundTaskQueue, QueueOfYouTubeAccounts>();
                    services.AddHostedService<ScheduledDownloadNewHostedService>();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseSerilog()
        ;
    }
}
