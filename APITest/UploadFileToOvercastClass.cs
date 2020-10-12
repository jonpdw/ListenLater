using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using APITest.Controllers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace APITest {
    static class UploadFileToOvercastClass {
        public static async Task UploadFileToOvercast(String fileLocation, string username,
            ILogger<StartProcessController> logger) {
            using (var w = new StopWatchWithNesting("Main")) {
                var userDetailsText = File.ReadAllText("user-data/UserDetails.json");
                var userDet =
                    JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(
                        userDetailsText);
                var baseAddress = new Uri("https://overcast.fm/");
                var cookieContainer = new CookieContainer();

                using (var handler = new HttpClientHandler() {CookieContainer = cookieContainer})
                using (var httpClient = new HttpClient(handler)) {
                    string uploadPolicy;
                    string uploadSignature;
                    string awsAccessKeyid;
                    string key;
                    // cookieContainer.Add(baseAddress,
                    //     new Cookie("o",
                    //         "Gf5Y1a265-YP_r7cOkIySI5nF6ampgNLZk-4ZJyGvNu9nWRtubsu0cSymBcIQdj1CjIp9TU2lP6iFMKS"));

                    // Login to Overcast
                    using (w.WatchInner("Login to Overcast")) {
                        using (var request =
                            new HttpRequestMessage(new HttpMethod("POST"), "https://overcast.fm/login")) {
                            var contentList = new List<string>();
                            try {
                                contentList.Add($"email={userDet[username]["Overcast"]["username"]}");
                                contentList.Add($"password={userDet[username]["Overcast"]["password"]}");
                            }
                            catch (KeyNotFoundException) {
                                logger.LogError("User details not found");
                            }

                            request.Content = new StringContent(string.Join("&", contentList));
                            request.Content.Headers.ContentType =
                                MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

                            HttpResponseMessage response = await httpClient.SendAsync(request);
                            response.EnsureSuccessStatusCode();
                            // Console.WriteLine(response);
                        }
                    }

                    // Get AWS Details form HTML 
                    using (var w1 = w.WatchInner("Get AWS Details")) {
                        using (var request =
                            new HttpRequestMessage(new HttpMethod("GET"), "https://overcast.fm/uploads")) {
                            HttpResponseMessage response;
                            using (w1.WatchInner("Get AWS Website Request")) {
                                response = await httpClient.SendAsync(request);
                                response.EnsureSuccessStatusCode();
                            }

                            using (w1.WatchInner("Regex")) {
                                var uploadsHtml = response.Content.ReadAsStringAsync().Result;

                                Regex uploadPolicyRegex =
                                    new Regex(
                                        @"<input type=""hidden"" id=""upload_policy"" name=""policy"" value=""(.*)""/>");
                                Regex uploadSignatureRegex =
                                    new Regex(
                                        @"<input type=""hidden"" id=""upload_signature"" name=""signature"" value=""(.*)""/>");
                                Regex awsAccessKeyIdRegex =
                                    new Regex(@"<input type=""hidden"" name=""AWSAccessKeyId"" value=""(.*)""/>");
                                Regex keyRegex =
                                    new Regex(@"<input type=""hidden"" name=""key"" value=""(.*\/.*\/).*""\/>");

                                Match uploadPolicyMatch = uploadPolicyRegex.Match(uploadsHtml);
                                Match uploadSignatureMatch = uploadSignatureRegex.Match(uploadsHtml);
                                Match awsAccessKeyIdMatch = awsAccessKeyIdRegex.Match(uploadsHtml);
                                Match keyMatch = keyRegex.Match(uploadsHtml);

                                if (uploadPolicyMatch.Success && uploadSignatureMatch.Success &&
                                    awsAccessKeyIdMatch.Success &&
                                    keyMatch.Success) {
                                    uploadPolicy = uploadPolicyMatch.Groups[1].Value;
                                    uploadSignature = uploadSignatureMatch.Groups[1].Value;
                                    awsAccessKeyid = awsAccessKeyIdMatch.Groups[1].Value;
                                    key = keyMatch.Groups[1].Value + Path.GetFileName(fileLocation);

                                    // Console.WriteLine($"Upload Policy {uploadPolicy}");
                                    // Console.WriteLine($"Upload Signature {uploadSignature}");
                                    // Console.WriteLine($"AWS Access Key {awsAccessKeyid}");
                                    // Console.WriteLine($"Key = {key}");
                                }
                                else {
                                    Console.WriteLine("Problem Extracting the Signature & Stuff");
                                    return;
                                }
                            }
                        }
                    }


                    using (var w2 = w.WatchInner("Tell Overcast + Send AWS file")) {
                        Task<HttpResponseMessage> responseAWS;
                        using (w2.WatchInner("Send AWS")) {
                            // Send file to AWS
                            var requestAWS = new HttpRequestMessage(new HttpMethod("POST"),
                                "https://uploads-overcast.s3.amazonaws.com/");
                            var multipartContentAWS = new MultipartFormDataContent();
                            multipartContentAWS.Add(new StringContent("uploads-overcast"), "bucket");
                            multipartContentAWS.Add(new StringContent(key), "key");
                            multipartContentAWS.Add(new StringContent(awsAccessKeyid), "AWSAccessKeyId");
                            multipartContentAWS.Add(new StringContent("authenticated-read"), "acl");
                            multipartContentAWS.Add(new StringContent(uploadPolicy), "policy");
                            multipartContentAWS.Add(new StringContent(uploadSignature), "signature");
                            multipartContentAWS.Add(new StringContent("audio/mpeg"), "Content-Type");
                            multipartContentAWS.Add(new ByteArrayContent(File.ReadAllBytes(fileLocation)), "file");
                            requestAWS.Content = multipartContentAWS;

                            responseAWS = httpClient.SendAsync(requestAWS);
                            // Console.WriteLine(response.Content.ReadAsStringAsync().Result);                        using () {
                        }

                        await responseAWS;
                        responseAWS.Result.EnsureSuccessStatusCode();

                        Task<HttpResponseMessage> responseOvercast;
                        using (w2.WatchInner("Tell Overcast")) {
                            // Tell Overcast things worked
                            var requestOvercast = new HttpRequestMessage(new HttpMethod("POST"),
                                "https://overcast.fm/podcasts/upload_succeeded/");
                            var multipartContentOvercast = new MultipartFormDataContent();
                            multipartContentOvercast.Add(new StringContent(key), "key");
                            requestOvercast.Content = multipartContentOvercast;

                            responseOvercast = httpClient.SendAsync(requestOvercast);
                            // Console.WriteLine(response.Content.ReadAsStringAsync().Result);
                        }

                        await responseOvercast;
                        responseOvercast.Result.EnsureSuccessStatusCode();
                        // File.Delete(fileLocation);
                    }

                    using (var request = new HttpRequestMessage(new HttpMethod("POST"),
                        "https://api.pushover.net/1/messages.json")) {
                        var multipartContent = new MultipartFormDataContent();
                        multipartContent.Add(new StringContent(userDet[username]["Pushover"]["token"]), "token");
                        multipartContent.Add(new StringContent(userDet[username]["Pushover"]["user"]), "user");
                        multipartContent.Add(new StringContent("Overcast: "), "title");
                        multipartContent.Add(new StringContent(Path.GetFileName(fileLocation)), "message");
                        request.Content = multipartContent;

                        var response = await httpClient.SendAsync(request);
                    }
                }
            }
        }
    }
}