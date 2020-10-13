using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ListenLater {
    public class DeQueueHostedService : BackgroundService
    {
   
        private readonly ILogger _logger;
        public IBackgroundTaskQueue TaskQueue { get; }
        private readonly IConfiguration _config;
        private readonly IHostEnvironment _hostingEnvironment;

        public DeQueueHostedService(IBackgroundTaskQueue taskQueue, ILoggerFactory loggerFactory, IHostEnvironment hostingEnvironment, IConfiguration config)
        {
            TaskQueue = taskQueue;
            _logger = loggerFactory.CreateLogger<DeQueueHostedService>();
            _hostingEnvironment = hostingEnvironment;
            _config = config;
        }


        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (false == stoppingToken.IsCancellationRequested) {
                var workItem = await TaskQueue.DequeueAsync(stoppingToken);
                try
                {
                    await workItem(stoppingToken);
                    TaskQueue.DecrementThingsFinishedFromQueue();
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, $"Error occurred executing {nameof(workItem)}.");
                }
            }
        }
    }

    public interface IBackgroundTaskQueue
    {
        void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);

        Task<Func<CancellationToken, Task>> DequeueAsync(
            CancellationToken cancellationToken);
        
        int ThingsFinishedFromQueue();

        void DecrementThingsFinishedFromQueue();
        
    }

    public class QueueOfYouTubeAccounts : IBackgroundTaskQueue
    {
        private ConcurrentQueue<Func<CancellationToken, Task>> _workItems = new ConcurrentQueue<Func<CancellationToken, Task>>();

        private SemaphoreSlim _signal = new SemaphoreSlim(0);
        
        private int thingsFinishedFromQueue = 0;

        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            _workItems.Enqueue(workItem);
            Interlocked.Increment(ref thingsFinishedFromQueue);
            _signal.Release();
        }

        public async Task<Func<CancellationToken, Task>> DequeueAsync( CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);

            return workItem;
        }

        public int ThingsFinishedFromQueue() {
            return thingsFinishedFromQueue;
        }
        public void DecrementThingsFinishedFromQueue() {
            Interlocked.Decrement(ref thingsFinishedFromQueue);
        }
    }
}