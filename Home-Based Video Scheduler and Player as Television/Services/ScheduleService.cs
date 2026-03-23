using Home_Based_Video_Scheduler_and_Player_as_Television.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Services
{
    public class ScheduleService
    {
        private static string FilePath => DataFolder.File("schedule.csv");

        public void Save(List<ScheduleItem> schedule)
        {
            var lines = schedule.Select(s =>
                $"{s.VideoId}|{s.VideoTitle}|{s.FilePath}|{s.StartTime:O}|{s.EndTime:O}|{s.DisplayTitle ?? ""}");
            File.WriteAllLines(FilePath, lines);
        }

        public List<ScheduleItem> Load()
        {
            if (!File.Exists(FilePath)) return new List<ScheduleItem>();

            var items = new List<ScheduleItem>();
            foreach (var line in File.ReadAllLines(FilePath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var p = line.Split('|');
                if (p.Length < 4) continue;
                try
                {
                    bool has5 = p.Length >= 5;
                    bool has6 = p.Length >= 6;
                    items.Add(new ScheduleItem
                    {
                        VideoId      = int.Parse(p[0]),
                        VideoTitle   = p[1],
                        FilePath     = has5 ? p[2] : string.Empty,
                        StartTime    = DateTime.Parse(p[has5 ? 3 : 2]),
                        EndTime      = DateTime.Parse(p[has5 ? 4 : 3]),
                        DisplayTitle = has6 ? p[5] : string.Empty
                    });
                }
                catch { }
            }
            return items;
        }
    }
}
