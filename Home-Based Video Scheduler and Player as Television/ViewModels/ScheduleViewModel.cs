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

        public ScheduleViewModel()
        {
            // Pre-fill start time from the true end of the last scheduled item
            if (Schedule.Any())
            {
                var last    = Schedule.OrderBy(s => s.StartTime).Last();
                var trueEnd = CommercialBreakStore.Instance.GetTrueEndTime(last);
                StartDate     = trueEnd.Date;
                StartTimeText = trueEnd.ToString("HH:mm:ss");
            }
        }

        private void AddSchedule()
        {
            if (SelectedVideo == null)
            {
                MessageBox.Show("Please select a video first.", "No Video Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var start    = GetStartDateTime();
            var videoEnd = start + SelectedVideo.Duration;

            // Sort schedule so we check in chronological order
            var sorted = Schedule.OrderBy(s => s.StartTime).ToList();

            foreach (var existing in sorted)
            {
                var existingTrueEnd = CommercialBreakStore.Instance.GetTrueEndTime(existing);

                bool newOverlapsExistingVideo = start < existing.EndTime    && videoEnd > existing.StartTime;
                bool newOverlapsExistingAds   = start < existingTrueEnd     && videoEnd > existing.EndTime;
                bool existingAdsOverlapNew    = existingTrueEnd > start     && existing.EndTime <= start;

                if (newOverlapsExistingVideo)
                {
                    // Hard video-on-video overlap — ask user
                    var r = MessageBox.Show(
                        $"'{SelectedVideo.Title}' overlaps with '{existing.VideoTitle}'.\n\nAdd anyway?",
                        "Overlap Detected", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (r != MessageBoxResult.Yes) return;
                    break;
                }

                if (existingAdsOverlapNew || newOverlapsExistingAds)
                {
                    // Commercial break of existing show runs into new show — hard block
                    MessageBox.Show(
                        $"Cannot schedule '{SelectedVideo.Title}' starting at {start:HH:mm:ss}.\n\n" +
                        $"'{existing.VideoTitle}' has commercials that run until {existingTrueEnd:HH:mm:ss}.\n\n" +
                        $"Please start the new show at {existingTrueEnd:HH:mm:ss} or later.",
                        "Commercial Break Conflict",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Also check: does the new show's videoEnd push into the START of the next show?
            // (i.e. the new show itself would need commercials that overlap the next show)
            var nextShow = sorted.FirstOrDefault(s => s.StartTime >= videoEnd);
            // This is fine — we only block if existing shows' TRUE ends conflict.
            // Future commercials on the new show will be caught by CommercialBreakViewModel.

            Schedule.Add(new ScheduleItem
            {
                VideoId      = SelectedVideo.Id,
                VideoTitle   = SelectedVideo.Title,
                FilePath     = SelectedVideo.FilePath,
                DisplayTitle = DisplayTitle.Trim(),
                StartTime    = start,
                EndTime      = videoEnd
            });

            _service.Save(Schedule.ToList());

            // Auto-fill next start from TrueEndTime of the show just added
            var newItem   = Schedule.OrderBy(s => s.StartTime).Last(s => s.StartTime == start);
            var trueEnd   = CommercialBreakStore.Instance.GetTrueEndTime(newItem);
            StartTimeText = trueEnd.ToString("HH:mm:ss");
            StartDate     = trueEnd.Date;
            DisplayTitle  = string.Empty;
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
