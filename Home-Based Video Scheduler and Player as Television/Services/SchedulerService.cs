using Home_Based_Video_Scheduler_and_Player_as_Television.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Services
{
    public class SchedulerService
    {
        private static SchedulerService _instance;
        public static SchedulerService Instance => _instance ??= new SchedulerService();
        private SchedulerService() { }

        public ObservableCollection<ScheduleItem> Schedule { get; set; }

        private bool _isPlayerOpen = false;
        public bool IsPlayerOpen
        {
            get => _isPlayerOpen;
            set
            {
                _isPlayerOpen = value;
                // When player closes, clear state so next open plays fresh
                if (!value) _currentlyPlaying = null;
            }
        }

        public event Action<ScheduleItem> VideoShouldPlay;
        public event Action<DateTime> Tick;

        private DispatcherTimer _timer;
        private ScheduleItem _currentlyPlaying;

        public void Start()
        {
            if (_timer != null) return;
            _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnTick;
            _timer.Start();
        }

        private void OnTick(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            Tick?.Invoke(now);

            if (Schedule == null || Schedule.Count == 0) { _currentlyPlaying = null; return; }

            var due = Schedule
                .Where(s => now >= s.StartTime && now < s.EndTime)
                .OrderBy(s => s.StartTime)
                .FirstOrDefault();

            if (due == null) { _currentlyPlaying = null; return; }

            // Same item — don't re-trigger
            if (_currentlyPlaying?.VideoId   == due.VideoId &&
                _currentlyPlaying?.StartTime == due.StartTime)
                return;

            _currentlyPlaying = due;

            if (_isPlayerOpen)
                VideoShouldPlay?.Invoke(due);
        }

        public ScheduleItem GetCurrentItem()
        {
            var now = DateTime.Now;
            return Schedule?
                .Where(s => now >= s.StartTime && now < s.EndTime)
                .OrderBy(s => s.StartTime)
                .FirstOrDefault();
        }

        public ScheduleItem GetNextItem()
        {
            var now = DateTime.Now;
            return Schedule?
                .Where(s => s.StartTime > now)
                .OrderBy(s => s.StartTime)
                .FirstOrDefault();
        }
    }
}
