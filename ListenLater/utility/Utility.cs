using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace ListenLater {
    public class Utility {
        public static async Task PushoverSendMessage(string message, string userToken, ILogger logger) {
            if (message == "Shutdown") { logger.LogWarning("Shutdown 1"); }
            using (var httpClient = new HttpClient()) {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"),
                    "https://api.pushover.net/1/messages.json")) {
                    
                    if (message == "Shutdown") { logger.LogWarning("Shutdown 2"); }
                    var multipartContent = new MultipartFormDataContent();
                    multipartContent.Add(new StringContent("a9az1mrgjfyqog4q695h14esy1itf6"), "token");
                    multipartContent.Add(new StringContent(userToken), "user");
                    multipartContent.Add(new StringContent("Overcast: "), "title");
                    multipartContent.Add(new StringContent(message), "message");
                    request.Content = multipartContent;
                    
                    if (message == "Shutdown") { logger.LogWarning("Shutdown 3"); }
                    var response = await httpClient.SendAsync(request);
                    if (message == "Shutdown") { logger.LogWarning("Shutdown 4"); }
                    
                }
            }
        }
        
        public static void PushoverSendMessageNotAsync(string message, string userToken, ILogger logger) {
            if (message == "Shutdown") { logger.LogWarning("Shutdown 1"); }
            using (var httpClient = new HttpClient()) {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"),
                    "https://api.pushover.net/1/messages.json")) {
                    
                    if (message == "Shutdown") { logger.LogWarning("Shutdown 2"); }
                    var multipartContent = new MultipartFormDataContent();
                    multipartContent.Add(new StringContent("a9az1mrgjfyqog4q695h14esy1itf6"), "token");
                    multipartContent.Add(new StringContent(userToken), "user");
                    multipartContent.Add(new StringContent("Overcast: "), "title");
                    multipartContent.Add(new StringContent(message), "message");
                    request.Content = multipartContent;
                    
                    if (message == "Shutdown") { logger.LogWarning("Shutdown 3 - before send request"); }
                    httpClient.SendAsync(request).Wait();
                    if (message == "Shutdown") { logger.LogWarning("Shutdown 4 - after send request"); }
                    
                }
            }
        }
        
        public static async Task PushoverSendMessage(string message, ILogger logger) { 
            await PushoverSendMessage(message, "u61ixtxjaqfbyfoqmvm221o2fovscx", logger);
        }
        
        public static void PushoverSendMessageNotAsync(string message, ILogger logger) { 
            PushoverSendMessageNotAsync(message, "u61ixtxjaqfbyfoqmvm221o2fovscx", logger);
        }
        
        public static Dictionary<string, Dictionary<string, Dictionary<string, string>>> GetUserDetailsDictionary() {
            var userDetailsText = File.ReadAllText("user-data/UserDetails.json");
            var userDet =
                JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(
                    userDetailsText);
            return userDet;
        }
    }
}