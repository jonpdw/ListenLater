using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace APITest.Controllers {
    [ApiController]
    [Route("api/go")]
    public class StartProcessController : ControllerBase {
        
        private readonly IHostEnvironment _hostingEnvironment;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ILogger<StartProcessController> _logger;

        public StartProcessController(IHostEnvironment hostingEnvironment, IBackgroundTaskQueue taskQueue, ILogger<StartProcessController> logger) {
            _hostingEnvironment = hostingEnvironment;
            _taskQueue = taskQueue;
            _logger = logger;
        }

        [Route("{username}")]
        public string Get(string username) {
            _logger.LogInformation(username);
            _logger.LogInformation("---Get Request---");
            string projectRootPath = _hostingEnvironment.ContentRootPath;
            _taskQueue.QueueBackgroundWorkItem(async cancelToken => {
                await DownloadYouTube.Do(projectRootPath, cancelToken, _logger);
            });
            return "Done";
        }
            
    };
}