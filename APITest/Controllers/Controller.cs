using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace APITest.Controllers {
    [ApiController]
    [Route("api/go")]
    public class StartProcessController : ControllerBase {
        private readonly IHostEnvironment _hostingEnvironment;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ILogger<StartProcessController> _logger;

        public StartProcessController(IHostEnvironment hostingEnvironment, IBackgroundTaskQueue taskQueue,
            ILogger<StartProcessController> logger) {
            _hostingEnvironment = hostingEnvironment;
            _taskQueue = taskQueue;
            _logger = logger;
        }

        [Route("{username}")]
        public string Get(string username) {
            _logger.LogInformation("---Get Request---");

            var userDetailsText = System.IO.File.ReadAllText("UserDetails.json");
            var userDet =
                JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(
                    userDetailsText);
            if (userDet.ContainsKey(username)) {
                string projectRootPath = _hostingEnvironment.ContentRootPath;
                _taskQueue.QueueBackgroundWorkItem(async cancelToken => {
                    await DownloadYouTube.Do(projectRootPath, cancelToken, _logger, username);
                });
                return "Done";
            }
            else {
                return "User Not Found";
            }
        }
    };
}