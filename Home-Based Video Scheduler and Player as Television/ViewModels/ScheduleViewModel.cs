using Home_Based_Video_Scheduler_and_Player_as_Television.Models;
using Home_Based_Video_Scheduler_and_Player_as_Television.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.ViewModels
{
    public class ScheduleViewModel : INotifyPropertyChanged
    {
        private readonly ScheduleService _service = new();

        // All scheduled items
        public ObservableCollection<ScheduleItem> Schedule { get; set; } = new();

        // All available videos to choose from
        public ObservableCollection<VideoModel> Videos { get; set; } = new();

        private VideoModel _selectedVideo;
        public VideoModel SelectedVideo
        {
            get => _selectedVideo;
            set
            {
                _selectedVideo = value;
                OnPropertyChanged(nameof(SelectedVideo));
            }
        }

        private DateTime _startTime = DateTime.Now;
        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                OnPropertyChanged(nameof(StartTime));
            }
        }

        private ScheduleItem _selectedScheduleItem;
        public ScheduleItem SelectedScheduleItem
        {
            get => _selectedScheduleItem;
            set
            {
                _selectedScheduleItem = value;
                OnPropertyChanged(nameof(SelectedScheduleItem));
            }
        }

        // Commands
        public ICommand AddScheduleCommand => new RelayCommand(AddSchedule);
        public ICommand RemoveScheduleCommand => new RelayCommand(RemoveSchedule);

        // Constructor
        public ScheduleViewModel()
        {
            LoadSchedule();
        }

        private void LoadSchedule()
        {
            var items = _service.Load();
            Schedule.Clear();
            foreach (var item in items)
                Schedule.Add(item);
        }

        private void AddSchedule()
        {
            if (SelectedVideo == null) return;

            var newItem = new ScheduleItem
            {
                VideoId = SelectedVideo.Id,
                StartTime = StartTime,
                EndTime = StartTime + SelectedVideo.Duration
            };

            Schedule.Add(newItem);
            SaveSchedule();
        }

        private void RemoveSchedule()
        {
            if (SelectedScheduleItem == null) return;

            Schedule.Remove(SelectedScheduleItem);
            SaveSchedule();
        }

        private void SaveSchedule()
        {
            _service.Save(Schedule.ToList());
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}