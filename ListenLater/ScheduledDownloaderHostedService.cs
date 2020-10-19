using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ListenLater {
    public class ScheduledDownloadNewHostedService : IHostedService, IDisposable {
        private readonly IHostEnvironment _hostingEnvironment;
        private readonly IBackgroundTaskQueue _taskQueue;
        private int executionCount = 0;
        private readonly ILogger<ScheduledDownloadNewHostedService> _logger;
        private Timer _timer;
        private readonly IConfiguration _config;

        public ScheduledDownloadNewHostedService(ILogger<ScheduledDownloadNewHostedService> logger, IHostEnvironment hostingEnvironment, IBackgroundTaskQueue taskQueue,
            IConfiguration config) {
            _hostingEnvironment = hostingEnvironment;
            _taskQueue = taskQueue;
            _logger = logger;
            _config = config;
        }

        public Task StartAsync(CancellationToken stoppingToken) {
            WrapMethod(stoppingToken);

            // _timer = new Timer(DoWork, null, TimeSpan.Zero, 
            //     TimeSpan.FromSeconds(10));
            return Task.CompletedTask;
        }

        private async void WrapMethod(CancellationToken stoppingToken) {
            _logger.LogInformation("Timed Hosted Service running.");
            while (false == stoppingToken.IsCancellationRequested) {
                DoWork(null);
                // await Task.Delay(1000 * 60 * 30, stoppingToken);
                await Task.Delay(1000 * 10, stoppingToken);
                // Don't try add new things to the queue if stuff is still downloading
                while (_taskQueue.ThingsFinishedFromQueue() > 0) {
                    await Task.Delay(1000 * 10);
                }

            }
        }

        private void DoWork(object state) {
            List<string> profilesToRun = File.ReadLines("user-data/RunEvery30Mins.txt").ToList();
            string projectRootPath = _hostingEnvironment.ContentRootPath;
            foreach (var username in profilesToRun) {
                var count = Interlocked.Increment(ref executionCount);
                _taskQueue.QueueBackgroundWorkItem(async cancelToken => {
                    // _logger.LogInformation("{Count} Started", count);
                    await DownloadYouTube.Do(projectRootPath, cancelToken, _logger, username, _config);
                    _logger.LogInformation("{Count} Finished", count);
                });
            }
        }

        public Task StopAsync(CancellationToken stoppingToken) {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose() {
            _timer?.Dispose();
        }
    }
}