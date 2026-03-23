using Home_Based_Video_Scheduler_and_Player_as_Television.Models;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.ViewModels
{
    public class VideoLibraryViewModel
    {
        public ObservableCollection<VideoModel> Videos { get; set; } = new();

        private int _idCounter = 1;

        // Currently selected video in the list
        private VideoModel _selectedVideo;
        public VideoModel SelectedVideo
        {
            get => _selectedVideo;
            set => _selectedVideo = value;
        }

        // Commands
        public ICommand AddVideoCommand => new RelayCommand(AddVideo);
        public ICommand RemoveVideoCommand => new RelayCommand(RemoveVideo);

        private void AddVideo()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Video Files|*.mp4;*.mkv;*.avi"
            };

            if (dialog.ShowDialog() == true)
            {
                string file = dialog.FileName;

                Videos.Add(new VideoModel
                {
                    Id = _idCounter++,
                    Title = Path.GetFileNameWithoutExtension(file),
                    FilePath = file,
                    Duration = TimeSpan.Zero // you can update with actual duration later
                });
            }
        }

        private void RemoveVideo()
        {
            if (SelectedVideo != null)
            {
                Videos.Remove(SelectedVideo);
                SelectedVideo = null; // clear selection
            }
        }
    }
}