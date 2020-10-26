using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ListenLater.Controllers;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using static ListenLater.UploadFileToOvercastClass;


namespace ListenLater {
    public class DownloadYouTube {
        public static async Task Do(string projectRootPath, CancellationToken cancelToken,
            ILogger logger, string username, IConfiguration _config) {
            var optionsGetPlaylist = new OptionSet {
                Cookies = projectRootPath + $"/user-data/cookies/{username}.txt",
                DumpJson = true,
                FlatPlaylist = true,
                PlaylistEnd = 2,
                DownloadArchive = projectRootPath + $"/user-data/already-downloaded-videos/{username}.txt",
            };
            
            var optionsGetFileName = new OptionSet {
                Output = "mp3/%(uploader)s - %(title)s.%(ext)s",
                Format = "worstaudio[ext=m4a]",
                GetFilename = true,
                NoProgress = true,
            };
            
            var optionsDownloadAudio = new OptionSet {
                Output = "mp3/%(uploader)s - %(title)s.%(ext)s",
                Format = "worstaudio[ext=m4a]",
            };
            
            var ytdl = new YoutubeDL();
            // set the path of the youtube-dl and FFmpeg if they're not in PATH or current directory
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                ytdl.YoutubeDLPath = "youtube-dl/youtube-dl-linux";
            }
            else {
                ytdl.YoutubeDLPath = "youtube-dl/youtube-dl-windows.exe";
            }
            // ytdl.FFmpegPath = "/usr/local/bin/ffmpeg";
            
            var watchLaterVideos = await ytdl.RunWithOptions(
                new[] {"https://www.youtube.com/playlist?disable_polymer=true&list=WL"}, optionsGetPlaylist,
                cancelToken);
            
            if (watchLaterVideos.Success == false) {
                Console.WriteLine("Problem getting watch later playlist");
                return;
            }
            
            logger.LogInformation($"Watch later has a length of {watchLaterVideos.Data.Length}");
            
            foreach (var videoString in watchLaterVideos.Data) {
                cancelToken.ThrowIfCancellationRequested();
                var videoDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(videoString);
                
                logger.LogInformation(videoDict["title"]);
                await Utility.PushoverSendMessage($"STARTED --- {videoDict["title"]}", Utility.GetUserDetailsDictionary()[username]["Pushover"]["user"], logger);
                
                var videoName = ytdl.RunWithOptions(new[] {videoDict["url"]}, optionsGetFileName, new CancellationToken());
                var videoDownload = await ytdl.RunWithOptions(new[] {videoDict["url"]}, optionsDownloadAudio, cancelToken);
                await videoName;

                if (NotSafeToExtractVideoName(logger, videoName)) {
                    UpdateAlreadyDownloadedVideos(projectRootPath, username, _config, videoDict);
                    continue;
                }
                var fileNameWithPath = videoName.Result.Data[0];

                fileNameWithPath = ChangeCharInFileName(fileNameWithPath, " ", "_");
                Console.WriteLine("Started Sponsorblock");
                try {
                    // await SponsorBlock.RemoveSegments("9wJn6nOEr7Q", "/mp3/Apple.m4a", projectRootPath);
                    fileNameWithPath = await SponsorBlock.RemoveSegments(videoDict["url"], fileNameWithPath, projectRootPath, logger);
                }
                catch (Exception e) {
                    logger.LogError(e.Message);
                    Console.WriteLine(e);
                    UpdateAlreadyDownloadedVideos(projectRootPath, username, _config, videoDict);
                    continue;
                }
                
                var fileNameWithPathReplaced = ChangeCharInFileName(fileNameWithPath, "_", " ");

                await UploadFileToOvercast(fileNameWithPathReplaced, username, logger);
                
                UpdateAlreadyDownloadedVideos(projectRootPath, username, _config, videoDict);

                File.Delete(fileNameWithPathReplaced);
            }
        }

        private static bool NotSafeToExtractVideoName(ILogger logger, Task<RunResult<string[]>> videoName) {
            try {
                var temp = videoName.Result.Data[0];
            }
            catch (IndexOutOfRangeException e) {
                logger.LogWarning(e.Message);
                return true;
            }

            return false;
        }

        private static void UpdateAlreadyDownloadedVideos(string projectRootPath, string username, IConfiguration _config, Dictionary<string, string> videoDict) {
            bool shouldAppend;
            shouldAppend = _config.GetValue<bool>("useAlreadyDownloadedVideosList");
            if (shouldAppend) {
                File.AppendAllText(projectRootPath + $"/user-data/already-downloaded-videos/{username}.txt",
                    $"youtube {videoDict["url"]}" + Environment.NewLine);
            }
        }

        private static string ChangeCharInFileName(string fileNameWithPath, string oldVal, string newVal) {
            var justFileName = Path.GetFileName(fileNameWithPath);
            var fileNameWithPathWithCharsReplaced = fileNameWithPath.Replace(oldVal, newVal);
            if (justFileName.Contains(oldVal)) {
                // If the file already exists it will cause the rename to throw an error. This can sometimes happen if an error happens in the Sponsorblock class and so the file from last time isn't deleted
                if (FileSystem.FileExists(fileNameWithPathWithCharsReplaced)==false)
                {
                    FileSystem.RenameFile($"{fileNameWithPath}", justFileName.Replace(oldVal, newVal));
                }
            }

            return fileNameWithPathWithCharsReplaced;
        }
    }
}