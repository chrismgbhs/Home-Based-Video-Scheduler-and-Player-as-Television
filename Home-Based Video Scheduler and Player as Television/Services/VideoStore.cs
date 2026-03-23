using Home_Based_Video_Scheduler_and_Player_as_Television.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Services
{
    public class VideoStore
    {
        private static VideoStore _instance;
        public static VideoStore Instance => _instance ??= new VideoStore();

        private static string VideosFile => DataFolder.File("videos.csv");

        private VideoStore()
        {
            LoadVideos();
            var saved = new ScheduleService().Load();
            foreach (var item in saved)
                Schedule.Add(item);
        }

        public ObservableCollection<VideoModel>   Videos   { get; } = new();
        public ObservableCollection<ScheduleItem> Schedule { get; } = new();

        private int _idCounter = 1;
        public int NextId() => _idCounter++;

        public VideoModel FindById(int id) =>
            Videos.FirstOrDefault(v => v.Id == id);

        public void SaveVideos()
        {
            var lines = Videos.Select(v =>
                $"{v.Id}|{v.Title}|{v.FilePath}|{v.Duration}");
            File.WriteAllLines(VideosFile, lines);
        }

        private void LoadVideos()
        {
            if (!File.Exists(VideosFile)) return;
            foreach (var line in File.ReadAllLines(VideosFile))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var p = line.Split('|');
                if (p.Length < 4) continue;
                try
                {
                    var v = new VideoModel
                    {
                        Id       = int.Parse(p[0]),
                        Title    = p[1],
                        FilePath = p[2],
                        Duration = TimeSpan.Parse(p[3])
                    };
                    if (v.Id >= _idCounter) _idCounter = v.Id + 1;
                    Videos.Add(v);
                }
                catch { }
            }
        }
    }
}
