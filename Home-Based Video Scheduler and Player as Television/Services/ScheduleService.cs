using Home_Based_Video_Scheduler_and_Player_as_Television.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Services
{
    public class ScheduleService
    {
        private readonly string filePath = "schedule.csv";

        public void Save(List<ScheduleItem> schedule)
        {
            var lines = schedule.Select(s =>
                $"{s.VideoId},{s.StartTime},{s.EndTime}");

            File.WriteAllLines(filePath, lines);
        }

        public List<ScheduleItem> Load()
        {
            if (!File.Exists(filePath)) return new List<ScheduleItem>();

            return File.ReadAllLines(filePath)
                .Select(line =>
                {
                    var parts = line.Split(',');
                    return new ScheduleItem
                    {
                        VideoId = int.Parse(parts[0]),
                        StartTime = DateTime.Parse(parts[1]),
                        EndTime = DateTime.Parse(parts[2])
                    };
                }).ToList();
        }
    }
}