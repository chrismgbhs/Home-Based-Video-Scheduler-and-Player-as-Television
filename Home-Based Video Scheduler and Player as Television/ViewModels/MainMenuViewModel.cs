using Home_Based_Video_Scheduler_and_Player_as_Television.Models;
using Home_Based_Video_Scheduler_and_Player_as_Television.Services;
using Home_Based_Video_Scheduler_and_Player_as_Television.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.ViewModels
{
    // ── Dashboard data model ──────────────────────────────────────────────────────

    public class DashboardDay
    {
        public string DateLabel   { get; set; }   // e.g. "TODAY  —  Monday, March 24"
        public bool   IsToday     { get; set; }
        public ObservableCollection<DashboardShow> Shows { get; set; } = new();
    }

    public class DashboardShow
    {
        public string Title         { get; set; }
        public string TimeRange     { get; set; }   // "08:00 — 09:30"
        public string ShowDuration  { get; set; }   // "1h 30m"
        public string AdTime        { get; set; }   // "5m 30s"  or "—"
        public string TrueEndTime   { get; set; }   // "09:35:30"
        public bool   IsNow         { get; set; }   // highlight if currently airing
        public bool   HasAds        { get; set; }
        public ObservableCollection<DashboardBreak> Breaks { get; set; } = new();
    }

    public class DashboardBreak
    {
        public string Offset       { get; set; }   // "@ 00:15:00"
        public string Title        { get; set; }
        public string Duration     { get; set; }
        public string EndsAt       { get; set; }
    }

    // ── ViewModel ─────────────────────────────────────────────────────────────────

    class MainMenuViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        public ICommand ViewPlayerCommand        { get; }
        public ICommand VideoLibraryCommand      { get; }
        public ICommand CommercialLibraryCommand { get; }
        public ICommand CommercialBreakCommand   { get; }
        public ICommand SchedulerCommand         { get; }
        public ICommand SettingsCommand          { get; }

        public int ScheduledCount  => VideoStore.Instance.Schedule.Count;
        public int VideoCount      => VideoStore.Instance.Videos.Count;
        public int CommercialCount => CommercialStore.Instance.Commercials.Count;

        public ObservableCollection<DashboardDay> ScheduleDays { get; } = new();

        public bool HasSchedule => VideoStore.Instance.Schedule.Count > 0;
        public bool NoSchedule  => !HasSchedule;

        public MainMenuViewModel()
        {
            ViewPlayerCommand        = new RelayCommand(ExecuteViewPlayer);
            VideoLibraryCommand      = new RelayCommand(ExecuteVideoLibrary);
            CommercialLibraryCommand = new RelayCommand(ExecuteCommercialLibrary);
            SchedulerCommand         = new RelayCommand(ExecuteScheduler);
            SettingsCommand          = new RelayCommand(ExecuteSettings);
            CommercialBreakCommand   = new RelayCommand(ExecuteCommercialBreak);

            BuildSchedule();

            // Re-build whenever schedule or breaks change
            VideoStore.Instance.Schedule.CollectionChanged           += (s, e) => Refresh();
            CommercialBreakStore.Instance.Breaks.CollectionChanged   += (s, e) => Refresh();
            CommercialStore.Instance.Commercials.CollectionChanged   += (s, e) => Refresh();
        }

        private void Refresh()
        {
            BuildSchedule();
            OnPropertyChanged(nameof(ScheduleDays));
            OnPropertyChanged(nameof(ScheduledCount));
            OnPropertyChanged(nameof(VideoCount));
            OnPropertyChanged(nameof(CommercialCount));
            OnPropertyChanged(nameof(HasSchedule));
            OnPropertyChanged(nameof(NoSchedule));
        }

        private void BuildSchedule()
        {
            ScheduleDays.Clear();
            var now = DateTime.Now;

            var grouped = VideoStore.Instance.Schedule
                .OrderBy(s => s.StartTime)
                .GroupBy(s => s.StartTime.Date);

            foreach (var group in grouped)
            {
                var date    = group.Key;
                var isToday = date.Date == now.Date;

                string dateLabel;
                if (isToday)
                    dateLabel = $"TODAY  —  {date:dddd, MMMM d}";
                else if (date.Date == now.Date.AddDays(1))
                    dateLabel = $"TOMORROW  —  {date:dddd, MMMM d}";
                else
                    dateLabel = date.ToString("dddd, MMMM d, yyyy").ToUpper();

                var day = new DashboardDay { DateLabel = dateLabel, IsToday = isToday };

                foreach (var item in group)
                {
                    var trueEnd   = CommercialBreakStore.Instance.GetTrueEndTime(item);
                    var showDur   = item.EndTime - item.StartTime;
                    var adDur     = trueEnd - item.EndTime;
                    var isNow     = now >= item.StartTime && now < trueEnd;

                    var show = new DashboardShow
                    {
                        Title        = item.EffectiveTitle,
                        TimeRange    = $"{item.StartTime:HH:mm}  —  {item.EndTime:HH:mm}",
                        ShowDuration = FormatDuration(showDur),
                        AdTime       = adDur > TimeSpan.Zero ? FormatDuration(adDur) : "—",
                        TrueEndTime  = adDur > TimeSpan.Zero ? trueEnd.ToString("HH:mm:ss") : "—",
                        IsNow        = isNow,
                        HasAds       = adDur > TimeSpan.Zero
                    };

                    // Add commercial break rows
                    var breaks = CommercialBreakStore.Instance.BreaksForShow(
                        item.FilePath, item.StartTime);

                    foreach (var b in breaks)
                    {
                        var commercial = CommercialStore.Instance.Commercials
                            .FirstOrDefault(c => c.Id == b.CommercialId);
                        var dur    = commercial?.Duration ?? TimeSpan.Zero;
                        var endsAt = b.Offset + dur;

                        show.Breaks.Add(new DashboardBreak
                        {
                            Offset   = $"@ {b.Offset:hh\\:mm\\:ss}",
                            Title    = b.CommercialTitle,
                            Duration = dur > TimeSpan.Zero ? FormatDuration(dur) : "—",
                            EndsAt   = $"ends {endsAt:hh\\:mm\\:ss}"
                        });
                    }

                    day.Shows.Add(show);
                }

                ScheduleDays.Add(day);
            }
        }

        private static string FormatDuration(TimeSpan d)
        {
            if (d.TotalSeconds <= 0) return "—";
            if (d.TotalHours >= 1)   return $"{(int)d.TotalHours}h {d.Minutes}m {d.Seconds}s";
            if (d.TotalMinutes >= 1) return $"{d.Minutes}m {d.Seconds}s";
            return $"{d.Seconds}s";
        }

        // ── Navigation ────────────────────────────────────────────────────────────

        private void ExecuteViewPlayer()
        {
            if (VideoStore.Instance.Schedule.Count == 0)
            {
                MessageBox.Show(
                    "No shows are scheduled yet.\n\nPlease add at least one video to the Schedule before opening the player.",
                    "No Schedule", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var existing = Application.Current.Windows.OfType<Player>().FirstOrDefault();
            if (existing != null) { existing.WindowState = WindowState.Normal; existing.Activate(); return; }
            try { new Player().Show(); }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open the video player.\n\nDetails: {ex.Message}",
                    "Player Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void OpenSingleInstance<T>() where T : Window, new()
        {
            var existing = Application.Current.Windows.OfType<T>().FirstOrDefault();
            if (existing != null) { existing.Activate(); return; }
            new T().Show();
        }

        private void ExecuteVideoLibrary()      => OpenSingleInstance<VideoLibrary>();
        private void ExecuteCommercialLibrary() => OpenSingleInstance<CommercialLibrary>();
        private void ExecuteScheduler()         => OpenSingleInstance<Schedule>();
        private void ExecuteCommercialBreak()   => OpenSingleInstance<CommercialBreakView>();
        private void ExecuteSettings()          => OpenSingleInstance<Settings>();
    }
}
