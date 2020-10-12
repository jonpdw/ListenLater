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
                
                var shouldAppend = _config.GetValue<bool>("useAlreadyDownloadedVideosList");
                if (shouldAppend) {
                    File.AppendAllText(projectRootPath + $"/user-data/already-downloaded-videos/{username}.txt",
                        $"youtube {videoDict["url"]}" + Environment.NewLine);
                }
                
                var videoName =
                    ytdl.RunWithOptions(new[] {videoDict["url"]}, optionsGetFileName, new CancellationToken());
                var videoDownload = await ytdl.RunWithOptions(new[] {videoDict["url"]}, optionsDownloadAudio,
                    cancelToken);
                await videoName;
                var fileName = videoName.Result.Data[0];
                await UploadFileToOvercast(fileName, username, logger);


                File.Delete(fileName);
            }
        }
    }
}