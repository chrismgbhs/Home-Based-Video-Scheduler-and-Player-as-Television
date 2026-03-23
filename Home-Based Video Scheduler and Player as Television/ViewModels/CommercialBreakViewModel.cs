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
    public class CommercialBreakViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ScheduleItem>    Shows       => VideoStore.Instance.Schedule;
        public ObservableCollection<CommercialModel> Commercials => CommercialStore.Instance.Commercials;

        private ScheduleItem _selectedShow;
        public ScheduleItem SelectedShow
        {
            get => _selectedShow;
            set
            {
                _selectedShow = value;
                OnPropertyChanged(nameof(SelectedShow));
                OnPropertyChanged(nameof(ShowDurationLabel));
                OnPropertyChanged(nameof(ShowDurationSeconds));
                RefreshBreaks();
            }
        }

        public double ShowDurationSeconds =>
            _selectedShow != null
                ? (_selectedShow.EndTime - _selectedShow.StartTime).TotalSeconds
                : 1;

        public string ShowDurationLabel =>
            _selectedShow != null
                ? $"{(int)(_selectedShow.EndTime - _selectedShow.StartTime).TotalMinutes} min"
                : "—";

        private ObservableCollection<CommercialBreakDisplay> _breaksForShow = new();
        public ObservableCollection<CommercialBreakDisplay> BreaksForShow
        {
            get => _breaksForShow;
            set { _breaksForShow = value; OnPropertyChanged(nameof(BreaksForShow)); }
        }

        private CommercialModel _selectedCommercial;
        public CommercialModel SelectedCommercial
        {
            get => _selectedCommercial;
            set { _selectedCommercial = value; OnPropertyChanged(nameof(SelectedCommercial)); }
        }

        private string _offsetText = "00:15:00";
        public string OffsetText
        {
            get => _offsetText;
            set { _offsetText = value; OnPropertyChanged(nameof(OffsetText)); }
        }

        private CommercialBreakDisplay _selectedBreak;
        public CommercialBreakDisplay SelectedBreak
        {
            get => _selectedBreak;
            set { _selectedBreak = value; OnPropertyChanged(nameof(SelectedBreak)); }
        }

        public ICommand AddBreakCommand    => new RelayCommand(AddBreak);
        public ICommand RemoveBreakCommand => new RelayCommand(RemoveBreak);

        public CommercialBreakViewModel() { }

        private void AddBreak()
        {
            if (SelectedShow == null)
            { MessageBox.Show("Select a show first.", "No Show Selected", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (SelectedCommercial == null)
            { MessageBox.Show("Select a commercial first.", "No Commercial Selected", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (!TimeSpan.TryParse(OffsetText, out var offset))
            { MessageBox.Show("Enter a valid offset (HH:mm:ss).", "Invalid Offset", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            var showDuration = SelectedShow.EndTime - SelectedShow.StartTime;

            // Offset must be within show video duration
            if (offset >= showDuration)
            {
                MessageBox.Show(
                    $"Offset {offset:hh\\:mm\\:ss} exceeds the show duration of {showDuration:hh\\:mm\\:ss}.",
                    "Invalid Offset", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // New commercial end = offset + its duration
            var newCommercialEnd = offset + SelectedCommercial.Duration;

            // Check overlap with existing breaks for this show
            var existingBreaks = CommercialBreakStore.Instance.Breaks
                .Where(b => b.ShowFilePath  == SelectedShow.FilePath &&
                            b.ShowStartTime == SelectedShow.StartTime)
                .ToList();

            foreach (var existing in existingBreaks)
            {
                var existingEnd = CommercialBreakStore.Instance.GetBreakEndOffset(existing);

                // [offset, newEnd) overlaps [existing.Offset, existingEnd) ?
                if (offset < existingEnd && newCommercialEnd > existing.Offset)
                {
                    MessageBox.Show(
                        $"This commercial overlaps with '{existing.CommercialTitle}' " +
                        $"(inserted at {existing.OffsetDisplay}, ends at {existingEnd:hh\\:mm\\:ss}).\n\n" +
                        $"Please choose an offset at {existingEnd:hh\\:mm\\:ss} or later.",
                        "Commercial Overlap",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Check: would adding this commercial push TrueEndTime past the next show's start?
            var nextShow = VideoStore.Instance.Schedule
                .Where(s => s.StartTime > SelectedShow.StartTime)
                .OrderBy(s => s.StartTime)
                .FirstOrDefault();

            if (nextShow != null)
            {
                // Simulate what TrueEndTime would be after adding this break
                var currentTrueEnd = CommercialBreakStore.Instance.GetTrueEndTime(SelectedShow);
                var newTrueEnd     = currentTrueEnd + SelectedCommercial.Duration;

                if (newTrueEnd > nextShow.StartTime)
                {
                    var maxAdTime = nextShow.StartTime - SelectedShow.EndTime;
                    var maxAdStr  = maxAdTime > TimeSpan.Zero
                        ? maxAdTime.ToString(@"hh\:mm\:ss")
                        : "0s";

                    MessageBox.Show(
                        $"Cannot add this commercial.\n\n" +
                        $"It would push '{SelectedShow.EffectiveTitle}' to end at {newTrueEnd:HH:mm:ss}, " +
                        $"which overlaps with '{nextShow.EffectiveTitle}' " +
                        $"starting at {nextShow.StartTime:HH:mm:ss}.\n\n" +
                        $"Maximum allowed ad time for this show: {maxAdStr}.",
                        "Conflict With Next Show",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            CommercialBreakStore.Instance.Breaks.Add(new CommercialBreak
            {
                Id                 = CommercialBreakStore.Instance.NextId(),
                ShowFilePath       = SelectedShow.FilePath,
                ShowStartTime      = SelectedShow.StartTime,
                CommercialId       = SelectedCommercial.Id,
                CommercialTitle    = SelectedCommercial.Title,
                CommercialFilePath = SelectedCommercial.FilePath,
                Offset             = offset
            });

            CommercialBreakStore.Instance.Save();
            RefreshBreaks();

            // Auto-advance offset past this commercial
            OffsetText = newCommercialEnd.ToString(@"hh\:mm\:ss");
        }

        private void RemoveBreak()
        {
            if (SelectedBreak == null) return;
            CommercialBreakStore.Instance.Breaks.Remove(SelectedBreak.Break);
            CommercialBreakStore.Instance.Save();
            RefreshBreaks();
        }

        private void RefreshBreaks()
        {
            if (_selectedShow == null) { BreaksForShow = new(); return; }

            var rawBreaks = CommercialBreakStore.Instance.BreaksForShow(
                _selectedShow.FilePath, _selectedShow.StartTime);

            // Wrap each break with its commercial duration for display
            var display = rawBreaks.Select(b =>
            {
                var commercial = CommercialStore.Instance.Commercials
                    .FirstOrDefault(c => c.Id == b.CommercialId);
                return new CommercialBreakDisplay(b, commercial?.Duration ?? TimeSpan.Zero);
            });

            BreaksForShow = new ObservableCollection<CommercialBreakDisplay>(display);
            OnPropertyChanged(nameof(ShowDurationSeconds));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Display wrapper for CommercialBreak that includes the commercial's duration.
    /// </summary>
    public class CommercialBreakDisplay
    {
        public CommercialBreak Break          { get; }
        public string CommercialTitle         => Break.CommercialTitle;
        public string OffsetDisplay           => Break.OffsetDisplay;
        public TimeSpan CommercialDuration    { get; }
        public string DurationDisplay         =>
            CommercialDuration == TimeSpan.Zero ? "—"
            : CommercialDuration.TotalHours >= 1
                ? $"{(int)CommercialDuration.TotalHours}h {CommercialDuration.Minutes}m {CommercialDuration.Seconds}s"
                : $"{CommercialDuration.Minutes}m {CommercialDuration.Seconds}s";

        public string EndsAtDisplay
        {
            get
            {
                var endsAt = Break.Offset + CommercialDuration;
                return endsAt.ToString(@"hh\:mm\:ss");
            }
        }

        public CommercialBreakDisplay(CommercialBreak b, TimeSpan duration)
        {
            Break               = b;
            CommercialDuration  = duration;
        }
    }
}
