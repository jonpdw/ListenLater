using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using APITest.Controllers;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using static APITest.UploadFileToOvercastClass;


namespace APITest {
    public class DownloadYouTube {
        public static async Task Do(string projectRootPath, CancellationToken cancelToken,
            ILogger<StartProcessController> logger, string username, IConfiguration _config) {
            var optionsGetPlaylist = new OptionSet {
                Cookies = projectRootPath + $"/cookies/{username}.txt",
                DumpJson = true,
                FlatPlaylist = true,
                PlaylistEnd = 3,
                DownloadArchive = projectRootPath + $"/already-downloaded-videos/{username}.txt",
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
                ytdl.YoutubeDLPath = "youtube-dl-linux";
            }
            else {
                ytdl.YoutubeDLPath = "youtube-dl-windows.exe";
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
                var videoName =
                    ytdl.RunWithOptions(new[] {videoDict["url"]}, optionsGetFileName, new CancellationToken());
                var videoDownload = await ytdl.RunWithOptions(new[] {videoDict["url"]}, optionsDownloadAudio,
                    cancelToken);
                await videoName;
                var fileName = videoName.Result.Data[0];
                await UploadFileToOvercast(fileName, username, logger);

                var shouldAppend = _config.GetValue<bool>("useAlreadyDownloadedVideosList");
                if (shouldAppend) {
                    File.AppendAllText(projectRootPath + $"/already-downloaded-videos/{username}.txt",
                        $"youtube {videoDict["url"]}" + Environment.NewLine);
                }

                File.Delete(fileName);
            }
        }
    }
}