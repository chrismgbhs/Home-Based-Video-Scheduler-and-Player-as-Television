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
        public ObservableCollection<ScheduleItem>   Shows       => VideoStore.Instance.Schedule;
        public ObservableCollection<CommercialModel> Commercials => CommercialStore.Instance.Commercials;

        // ── Selected show ─────────────────────────────────────────────────────────
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

        // Total duration of selected show in seconds (drives timeline width math)
        public double ShowDurationSeconds =>
            _selectedShow != null
                ? (_selectedShow.EndTime - _selectedShow.StartTime).TotalSeconds
                : 1;

        public string ShowDurationLabel =>
            _selectedShow != null
                ? $"{(int)(_selectedShow.EndTime - _selectedShow.StartTime).TotalMinutes} min"
                : "—";

        // ── Breaks ────────────────────────────────────────────────────────────────
        private ObservableCollection<CommercialBreak> _breaksForShow = new();
        public ObservableCollection<CommercialBreak> BreaksForShow
        {
            get => _breaksForShow;
            set { _breaksForShow = value; OnPropertyChanged(nameof(BreaksForShow)); }
        }

        // ── Add break inputs ──────────────────────────────────────────────────────
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

        private CommercialBreak _selectedBreak;
        public CommercialBreak SelectedBreak
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
            if (offset >= showDuration)
            { MessageBox.Show("Offset exceeds show duration.", "Invalid Offset", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            bool duplicate = CommercialBreakStore.Instance.Breaks.Any(b =>
                b.ShowFilePath == SelectedShow.FilePath &&
                b.ShowStartTime == SelectedShow.StartTime &&
                b.Offset == offset);
            if (duplicate)
            { MessageBox.Show("A break already exists at that offset.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

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
            OffsetText = (offset + SelectedCommercial.Duration).ToString(@"hh\:mm\:ss");
        }

        private void RemoveBreak()
        {
            if (SelectedBreak == null) return;
            CommercialBreakStore.Instance.Breaks.Remove(SelectedBreak);
            CommercialBreakStore.Instance.Save();
            RefreshBreaks();
        }

        private void RefreshBreaks()
        {
            if (_selectedShow == null) { BreaksForShow = new(); return; }
            BreaksForShow = CommercialBreakStore.Instance.BreaksForShow(
                _selectedShow.FilePath, _selectedShow.StartTime);
            OnPropertyChanged(nameof(ShowDurationSeconds));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
