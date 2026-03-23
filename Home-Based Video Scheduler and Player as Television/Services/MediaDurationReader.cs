using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Services
{
    /// <summary>
    /// Reads durations for a batch of files using a single LibVLC instance.
    /// Much faster than creating a new LibVLC per file.
    /// </summary>
    public static class MediaDurationReader
    {
        public static Dictionary<string, TimeSpan> ReadBatch(IEnumerable<string> filePaths)
        {
            var result = new Dictionary<string, TimeSpan>();
            try
            {
                using var libVLC = new LibVLC();
                foreach (var path in filePaths)
                {
                    try
                    {
                        using var media = new Media(libVLC, new Uri(path));
                        media.Parse(MediaParseOptions.ParseLocal).Wait();
                        result[path] = TimeSpan.FromMilliseconds(media.Duration);
                    }
                    catch { result[path] = TimeSpan.Zero; }
                }
            }
            catch { }
            return result;
        }
    }
}
