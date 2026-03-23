using Home_Based_Video_Scheduler_and_Player_as_Television.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Services
{
    public class CommercialBreakStore
    {
        private static CommercialBreakStore _instance;
        public static CommercialBreakStore Instance => _instance ??= new CommercialBreakStore();

        private static string FilePath => DataFolder.File("commercial_breaks.csv");

        private CommercialBreakStore() { Load(); }

        public ObservableCollection<CommercialBreak> Breaks { get; } = new();

        private int _idCounter = 1;
        public int NextId() => _idCounter++;

        public void Save()
        {
            var lines = Breaks.Select(b =>
                $"{b.Id}|{b.ShowFilePath}|{b.ShowStartTime:O}|{b.CommercialId}|{b.CommercialTitle}|{b.CommercialFilePath}|{b.Offset}");
            File.WriteAllLines(FilePath, lines);
        }

        private void Load()
        {
            if (!File.Exists(FilePath)) return;
            foreach (var line in File.ReadAllLines(FilePath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var p = line.Split('|');
                if (p.Length < 7) continue;
                try
                {
                    var b = new CommercialBreak
                    {
                        Id                 = int.Parse(p[0]),
                        ShowFilePath       = p[1],
                        ShowStartTime      = DateTime.Parse(p[2]),
                        CommercialId       = int.Parse(p[3]),
                        CommercialTitle    = p[4],
                        CommercialFilePath = p[5],
                        Offset             = TimeSpan.Parse(p[6])
                    };
                    if (b.Id >= _idCounter) _idCounter = b.Id + 1;
                    Breaks.Add(b);
                }
                catch { }
            }
        }

        public ObservableCollection<CommercialBreak> BreaksForShow(string showFilePath, DateTime showStartTime)
        {
            return new ObservableCollection<CommercialBreak>(
                Breaks
                    .Where(b => b.ShowFilePath  == showFilePath &&
                                b.ShowStartTime == showStartTime)
                    .OrderBy(b => b.Offset));
        }
    }
}
