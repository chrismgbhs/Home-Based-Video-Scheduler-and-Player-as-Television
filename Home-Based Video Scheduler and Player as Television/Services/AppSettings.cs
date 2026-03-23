using System;
using System.ComponentModel;
using System.IO;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Services
{
    public class AppSettings : INotifyPropertyChanged
    {
        private static AppSettings _instance;
        public static AppSettings Instance => _instance ??= new AppSettings();
        private AppSettings() { Load(); }

        private static string FilePath => DataFolder.File("settings.ini");

        private string _logoPath = string.Empty;
        public string LogoPath
        {
            get => _logoPath;
            set { _logoPath = value; OnPropertyChanged(nameof(LogoPath)); }
        }

        private double _logoSize = 80;
        public double LogoSize
        {
            get => _logoSize;
            set { _logoSize = Math.Max(40, Math.Min(200, value)); OnPropertyChanged(nameof(LogoSize)); }
        }

        private double _logoOpacity = 0.85;
        public double LogoOpacity
        {
            get => _logoOpacity;
            set { _logoOpacity = Math.Max(0.1, Math.Min(1.0, value)); OnPropertyChanged(nameof(LogoOpacity)); }
        }

        private bool _overlayEnabled = true;
        public bool OverlayEnabled
        {
            get => _overlayEnabled;
            set { _overlayEnabled = value; OnPropertyChanged(nameof(OverlayEnabled)); }
        }

        private bool _autoCommercialsEnabled = true;
        public bool AutoCommercialsEnabled
        {
            get => _autoCommercialsEnabled;
            set { _autoCommercialsEnabled = value; OnPropertyChanged(nameof(AutoCommercialsEnabled)); }
        }

        public void Save()
        {
            File.WriteAllLines(FilePath, new[]
            {
                $"LogoPath={_logoPath}",
                $"LogoSize={_logoSize}",
                $"LogoOpacity={_logoOpacity}",
                $"OverlayEnabled={_overlayEnabled}",
                $"AutoCommercialsEnabled={_autoCommercialsEnabled}"
            });
        }

        private void Load()
        {
            if (!File.Exists(FilePath)) return;
            foreach (var line in File.ReadAllLines(FilePath))
            {
                var idx = line.IndexOf('=');
                if (idx < 0) continue;
                var key = line.Substring(0, idx).Trim();
                var val = line.Substring(idx + 1).Trim();
                switch (key)
                {
                    case "LogoPath":               _logoPath = val; break;
                    case "LogoSize":               if (double.TryParse(val, out var sz)) _logoSize = sz; break;
                    case "LogoOpacity":            if (double.TryParse(val, out var op)) _logoOpacity = op; break;
                    case "OverlayEnabled":         if (bool.TryParse(val, out var ov))   _overlayEnabled = ov; break;
                    case "AutoCommercialsEnabled": if (bool.TryParse(val, out var ac))   _autoCommercialsEnabled = ac; break;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
