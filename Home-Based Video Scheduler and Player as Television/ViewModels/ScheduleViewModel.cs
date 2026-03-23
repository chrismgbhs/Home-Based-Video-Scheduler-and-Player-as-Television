using Home_Based_Video_Scheduler_and_Player_as_Television.Models;
using Home_Based_Video_Scheduler_and_Player_as_Television.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.ViewModels
{
    public class ScheduleViewModel : INotifyPropertyChanged
    {
        private readonly ScheduleService _service = new();

        public ObservableCollection<ScheduleItem> Schedule => VideoStore.Instance.Schedule;
        public ObservableCollection<VideoModel>   Videos   => VideoStore.Instance.Videos;

        private VideoModel _selectedVideo;
        public VideoModel SelectedVideo
        {
            get => _selectedVideo;
            set { _selectedVideo = value; OnPropertyChanged(nameof(SelectedVideo)); }
        }

        private DateTime _startDate = DateTime.Today;
        public DateTime StartDate
        {
            get => _startDate;
            set { _startDate = value; OnPropertyChanged(nameof(StartDate)); }
        }

        private string _startTimeText = DateTime.Now.ToString("HH:mm:ss");
        public string StartTimeText
        {
            get => _startTimeText;
            set { _startTimeText = value; OnPropertyChanged(nameof(StartTimeText)); }
        }

        // Optional custom on-screen title — blank means use video filename
        private string _displayTitle = string.Empty;
        public string DisplayTitle
        {
            get => _displayTitle;
            set { _displayTitle = value; OnPropertyChanged(nameof(DisplayTitle)); }
        }

        private DateTime GetStartDateTime()
        {
            if (TimeSpan.TryParse(StartTimeText, out var t))
                return StartDate.Date + t;
            return StartDate.Date + DateTime.Now.TimeOfDay;
        }

        private ScheduleItem _selectedScheduleItem;
        public ScheduleItem SelectedScheduleItem
        {
            get => _selectedScheduleItem;
            set { _selectedScheduleItem = value; OnPropertyChanged(nameof(SelectedScheduleItem)); }
        }

        public ICommand AddScheduleCommand    => new RelayCommand(AddSchedule);
        public ICommand RemoveScheduleCommand => new RelayCommand(RemoveSchedule);

        public ScheduleViewModel() { }

        private void AddSchedule()
        {
            if (SelectedVideo == null)
            {
                MessageBox.Show("Please select a video first.", "No Video Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var start = GetStartDateTime();
            var end   = start + SelectedVideo.Duration;

            bool overlaps = Schedule.Any(s => start < s.EndTime && end > s.StartTime);
            if (overlaps)
            {
                var r = MessageBox.Show(
                    "This time slot overlaps with an existing entry. Add anyway?",
                    "Overlap Detected", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r != MessageBoxResult.Yes) return;
            }

            Schedule.Add(new ScheduleItem
            {
                VideoId      = SelectedVideo.Id,
                VideoTitle   = SelectedVideo.Title,
                FilePath     = SelectedVideo.FilePath,
                DisplayTitle = DisplayTitle.Trim(),
                StartTime    = start,
                EndTime      = end
            });

            _service.Save(Schedule.ToList());

            StartTimeText = end.ToString("HH:mm:ss");
            StartDate     = end.Date;
            DisplayTitle  = string.Empty;  // reset for next entry
        }

        private void RemoveSchedule()
        {
            if (SelectedScheduleItem == null) return;
            Schedule.Remove(SelectedScheduleItem);
            _service.Save(Schedule.ToList());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
