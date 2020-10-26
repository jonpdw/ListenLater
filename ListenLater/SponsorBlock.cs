using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Xabe.FFmpeg;

namespace ListenLater {
    public class SponsorBlock {
        public static async Task<string> RemoveSegments(string videoId, string inputFileRelativePath, string projectRootPath, ILogger logger) {
            var inputFileFullPath = $"{projectRootPath}/{inputFileRelativePath}";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                FFmpeg.SetExecutablesPath($"{projectRootPath}/ffmpeg-binarys", "ffmpeg-i686", "ffprobe-i686");
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                // FFmpeg.SetExecutablesPath($"{projectRootPath}/ffmpeg-binarys", "ffmpeg", "ffprobe");
            }

            IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(inputFileFullPath);
            var lengthVideoInSeconds = mediaInfo.Duration.TotalSeconds;

            // Get Data from Sponsorblock api
            string apiResponseString;
            using (HttpClient client = new HttpClient()) {
                // apiResponseString = await client.GetStringAsync($"http://sponsor.ajay.app/api/skipSegments?videoID={videoId}");
                var request = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"http://sponsor.ajay.app/api/skipSegments?videoID={videoId}"));
                if (!request.IsSuccessStatusCode) {
                    logger.LogError("SponsorBlock API 404");
                    await Utility.PushoverSendMessage("SponsorBlock API 404", logger);
                    return inputFileRelativePath;
                }

                apiResponseString = await request.Content.ReadAsStringAsync();
            }


            var segmentsToSkip = JsonConvert.DeserializeObject<List<Segment>>(apiResponseString);

            // Creates list like this [0, startSegment1, finishSegment1, startSegment2, finishSegment2, duration]
            var allStartStopTimes = segmentsToSkip.SelectMany(x => x.segment).ToList();
            allStartStopTimes.Insert(0, 0);
            allStartStopTimes.Add(lengthVideoInSeconds);

            if (allStartStopTimes.Count % 2 != 0) {
                throw new Exception("Total number of start and stop times should be even");
            }

            // Makes list like this [[0, startSegment1], [finishSegment1, startSegment2], [finishSegment2, duration]]
            var audioSegmentTimePairs = new List<AudioSegment>();
            for (int i = 1; i < allStartStopTimes.Count; i += 2) {
                audioSegmentTimePairs.Add(new AudioSegment(allStartStopTimes[i - 1], allStartStopTimes[i]));
            }


            // TODO Fix files not being able to have spaces - When the file location has a space in it this seems to crash and I can't escape it by puting either single or double quotes around it e.g. '{fileLocation}' or \"{fileLocation}\"
            var arguments = $"-i {inputFileFullPath} -filter_complex \"";
            var idForEachSegment = 0;
            var listOfIds = new List<string>();
            foreach (var s in audioSegmentTimePairs) {
                arguments += $"[0:a]atrim=start={s.Start}:end={s.End},asetpts=PTS-STARTPTS[{idForEachSegment}];";
                listOfIds.Add($"[{idForEachSegment}]");
                idForEachSegment++;
            }

            // constructs a line like this: [1][2][3]concat=n=3:v=0:a=1[f]
            arguments += $"{String.Join("", listOfIds)}concat=n={listOfIds.Count}:v=0:a=1[f]\" ";

            // construct outpput file name
            var fileName = Path.GetFileNameWithoutExtension(inputFileFullPath);
            var directory = Directory.GetParent(inputFileFullPath).FullName;
            var outputFileName = $"{directory}/{fileName}-SponsorBlocked.m4a";
            arguments += $"-map [f] {outputFileName}";

            await Utility.PushoverSendMessage("Started Conversion", logger);

            var conversionResult1 = await FFmpeg.Conversions.New().Start(arguments);

            await Utility.PushoverSendMessage("Finished Conversion", logger);

            File.Delete($"{directory}/{fileName}.m4a");
            return outputFileName;
        }
    }

    class AudioSegment {
        public AudioSegment(double start, double end) {
            this.Start = start;
            this.End = end;
        }

        public double Start { get; }
        public double End { get; }
    }

    public class Segment {
        public string category { get; set; }
        public List<double> segment { get; set; }
        public string UUID { get; set; }
    }
}