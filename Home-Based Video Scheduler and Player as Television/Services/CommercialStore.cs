using Home_Based_Video_Scheduler_and_Player_as_Television.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Services
{
    public class CommercialStore
    {
        private static CommercialStore _instance;
        public static CommercialStore Instance => _instance ??= new CommercialStore();

        private static string FilePath => DataFolder.File("commercials.csv");

        private CommercialStore() { Load(); }

        public ObservableCollection<CommercialModel> Commercials { get; } = new();

        private int _idCounter = 1;
        public int NextId() => _idCounter++;

        public void Save()
        {
            var lines = Commercials.Select(c =>
                $"{c.Id}|{c.Title}|{c.FilePath}|{c.Duration}|{c.Weight}");
            File.WriteAllLines(FilePath, lines);
        }

        private void Load()
        {
            if (!File.Exists(FilePath)) return;
            foreach (var line in File.ReadAllLines(FilePath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var p = line.Split('|');
                if (p.Length < 4) continue;
                try
                {
                    var c = new CommercialModel
                    {
                        Id       = int.Parse(p[0]),
                        Title    = p[1],
                        FilePath = p[2],
                        Duration = System.TimeSpan.Parse(p[3]),
                        Weight   = p.Length >= 5 && int.TryParse(p[4], out var w) ? w : 1
                    };
                    if (c.Id >= _idCounter) _idCounter = c.Id + 1;
                    Commercials.Add(c);
                }
                catch { }
            }
        }
    }
}
