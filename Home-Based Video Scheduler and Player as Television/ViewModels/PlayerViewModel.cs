using LibVLCSharp.Shared;
using System;
using System.ComponentModel;
using System.IO;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.ViewModels
{
    public class PlayerViewModel : INotifyPropertyChanged
    {
        private LibVLC _libVLC;
        public MediaPlayer MediaPlayer { get; private set; }

        private string _nowPlaying;
        public string NowPlaying
        {
            get => _nowPlaying;
            set
            {
                _nowPlaying = value;
                OnPropertyChanged(nameof(NowPlaying));
            }
        }

        private string _logoPath;
        public string LogoPath
        {
            get => _logoPath;
            set
            {
                _logoPath = value;
                OnPropertyChanged(nameof(LogoPath));
            }
        }

        public PlayerViewModel()
        {
            Core.Initialize();

            _libVLC = new LibVLC();
            MediaPlayer = new MediaPlayer(_libVLC);
            PlayVideo("C:/Users/chris/Videos/2025 DRAMAS.mp4");
        }

        // 🎬 PLAY VIDEO
        public void PlayVideo(string path)
        {
            if (!File.Exists(path)) return;

            var media = new Media(_libVLC, new Uri(path));

            // Auto-load subtitle
            string subtitlePath = Path.ChangeExtension(path, ".srt");
            if (File.Exists(subtitlePath))
            {
                media.AddOption($":sub-file={subtitlePath}");
            }

            MediaPlayer.Play(media);

            NowPlaying = Path.GetFileName(path);
        }

        public void Pause() => MediaPlayer?.Pause();
        public void Stop() => MediaPlayer?.Stop();
        public void Resume() => MediaPlayer?.Play();

        public long GetCurrentTime() => MediaPlayer?.Time ?? 0;

        public void SetTime(long time)
        {
            if (MediaPlayer != null)
                MediaPlayer.Time = time;
        }

        // 🔔 REQUIRED FOR BINDING
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}