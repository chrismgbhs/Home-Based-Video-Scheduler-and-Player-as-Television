using LibVLCSharp.Shared;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.ViewModels
{
    public class PlayerViewModel : INotifyPropertyChanged
    {
        public async void ShowNowPlayingTemporarily()
        {
            NowPlayingVisible = true;
            await Task.Delay(5000);
            NowPlayingVisible = false;
        }

        private LibVLC _libVLC;
        public MediaPlayer MediaPlayer { get; private set; }

        private BitmapImage _logoImage;
        public BitmapImage LogoImage
        {
            get => _logoImage;
            set
            {
                _logoImage = value;
                OnPropertyChanged(nameof(LogoImage));
            }
        }

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

        public PlayerViewModel()
        {
            Core.Initialize();

            _libVLC = new LibVLC();
            MediaPlayer = new MediaPlayer(_libVLC);
            PlayVideo("C:/Users/chris/Videos/Riko Valentine's.mp4");
            LogoImage = new BitmapImage(new Uri("C:/Users/chris/Pictures/Logo and Signatures/Chriz Logo.png"));
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

            NowPlaying = Path.GetFileNameWithoutExtension(path);
            ShowNowPlayingTemporarily();
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

        private bool _nowPlayingVisible;
        public bool NowPlayingVisible
        {
            get => _nowPlayingVisible;
            set
            {
                _nowPlayingVisible = value;
                OnPropertyChanged(nameof(NowPlayingVisible));
            }
        }





        // 🔔 REQUIRED FOR BINDING
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}