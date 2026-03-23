using Home_Based_Video_Scheduler_and_Player_as_Television.Models;
using Home_Based_Video_Scheduler_and_Player_as_Television.Services;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.ViewModels
{
    public class PlayerViewModel : INotifyPropertyChanged
    {
        private static PlayerViewModel _instance;
        public static PlayerViewModel Instance => _instance ??= new PlayerViewModel();

        private readonly LibVLC _libVLC;
        public MediaPlayer MediaPlayer { get; private set; }

        // Track current Media so we can dispose it when switching
        private Media _currentMedia;

        // ── Overlay bindings ──────────────────────────────────────────────────────
        private BitmapImage _logoImage;
        public BitmapImage LogoImage
        {
            get => _logoImage;
            set { _logoImage = value; OnPropertyChanged(nameof(LogoImage)); }
        }

        private string _nowPlaying;
        public string NowPlaying
        {
            get => _nowPlaying;
            set { if (_nowPlaying == value) return; _nowPlaying = value; OnPropertyChanged(nameof(NowPlaying)); }
        }

        private string _clockText;
        public string ClockText
        {
            get => _clockText;
            set { if (_clockText == value) return; _clockText = value; OnPropertyChanged(nameof(ClockText)); }
        }

        private string _upNext;
        public string UpNext
        {
            get => _upNext;
            set { if (_upNext == value) return; _upNext = value; OnPropertyChanged(nameof(UpNext)); }
        }

        private bool _isOverlayVisible = true;
        public bool IsOverlayVisible
        {
            get => _isOverlayVisible;
            set { if (_isOverlayVisible == value) return; _isOverlayVisible = value; OnPropertyChanged(nameof(IsOverlayVisible)); }
        }

        // ── Mid-show commercial break state ───────────────────────────────────────
        private ScheduleItem _currentScheduleItem;
        private long _savedShowPosition;
        private bool _playingMidShowCommercial;
        private List<CommercialBreak> _pendingBreaks;
        private DispatcherTimer _breakWatcher;

        // ── Gap filler state ──────────────────────────────────────────────────────
        private bool _inGap;
        private List<GapFillerService.FillerSlot> _gapQueue;
        private int _gapIndex;
        private DispatcherTimer _gapCutTimer;

        // ── Constructor ───────────────────────────────────────────────────────────
        private PlayerViewModel()
        {
            Core.Initialize();
            _libVLC     = new LibVLC();
            MediaPlayer = new MediaPlayer(_libVLC);
            MediaPlayer.EndReached += OnMediaEndReached;

            var scheduler = SchedulerService.Instance;
            scheduler.Schedule        = VideoStore.Instance.Schedule;
            scheduler.VideoShouldPlay += OnVideoShouldPlay;
            scheduler.Tick            += OnSchedulerTick;
            scheduler.Start();

            UpdateUpNext();
            LoadSavedLogo();
        }

        private void LoadSavedLogo()
        {
            SetLogo(AppSettings.Instance.LogoPath);
        }

        // ── Window ready ──────────────────────────────────────────────────────────
        public void OnPlayerWindowReady()
        {
            var current = SchedulerService.Instance.GetCurrentItem();
            if (current != null)
            {
                StopGapFiller();
                StartShow(current);
            }
            else
            {
                TryStartGapFiller();
            }
            UpdateUpNext();
        }

        // ── Scheduler callbacks ───────────────────────────────────────────────────
        private void OnVideoShouldPlay(ScheduleItem item)
        {
            StopGapFiller();
            StartShow(item);
            UpdateUpNext();
        }

        private void OnSchedulerTick(DateTime now)
        {
            ClockText = now.ToString("HH:mm:ss");

            // Only check for gap if we're not in a show and not already filling
            if (!_inGap && !_playingMidShowCommercial &&
                SchedulerService.Instance.GetCurrentItem() == null)
                TryStartGapFiller();
        }

        // ── Show playback ─────────────────────────────────────────────────────────
        private void StartShow(ScheduleItem item)
        {
            _playingMidShowCommercial = false;
            _currentScheduleItem     = item;
            IsOverlayVisible         = AppSettings.Instance.OverlayEnabled;

            _pendingBreaks = CommercialBreakStore.Instance.Breaks
                .Where(b => b.ShowFilePath  == item.FilePath &&
                            b.ShowStartTime == item.StartTime)
                .OrderBy(b => b.Offset)
                .Select(b => { b.HasFired = false; return b; })
                .ToList();

            var elapsed = DateTime.Now - item.StartTime;
            var seekTo  = elapsed > TimeSpan.Zero ? elapsed : TimeSpan.Zero;

            foreach (var b in _pendingBreaks)
                if (b.Offset < seekTo) b.HasFired = true;

            PlayVideo(item.FilePath, seekTo);
            NowPlaying = item.EffectiveTitle;
            StartBreakWatcher();
        }

        // ── Mid-show break watcher — 1s interval is enough, reduces CPU ──────────
        private void StartBreakWatcher()
        {
            _breakWatcher?.Stop();
            _breakWatcher = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _breakWatcher.Tick += OnBreakWatcherTick;
            _breakWatcher.Start();
        }

        private void StopBreakWatcher()
        {
            _breakWatcher?.Stop();
            _breakWatcher = null;
        }

        private void OnBreakWatcherTick(object sender, EventArgs e)
        {
            if (_playingMidShowCommercial || _pendingBreaks == null || _pendingBreaks.Count == 0) return;
            if (!MediaPlayer.IsPlaying) return;

            var currentPos = TimeSpan.FromMilliseconds(MediaPlayer.Time);
            var due = _pendingBreaks.FirstOrDefault(b => !b.HasFired && currentPos >= b.Offset);
            if (due == null) return;

            due.HasFired = true;
            FireMidShowCommercial(due);
        }

        private void FireMidShowCommercial(CommercialBreak b)
        {
            if (!File.Exists(b.CommercialFilePath)) return;

            _savedShowPosition        = MediaPlayer.Time;
            _playingMidShowCommercial = true;
            IsOverlayVisible          = false;
            NowPlaying                = $"[Ad] {b.CommercialTitle}";

            MediaPlayer.Pause();

            // Short delay then play — use Task.Run to avoid blocking UI thread
            Task.Run(async () =>
            {
                await Task.Delay(200);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var media = CreateMedia(b.CommercialFilePath);
                    MediaPlayer.Play(media);
                });
            });
        }

        // ── Gap filler ────────────────────────────────────────────────────────────
        private void TryStartGapFiller()
        {
            if (!AppSettings.Instance.AutoCommercialsEnabled) return;
            if (_inGap) return;
            if (!CommercialStore.Instance.Commercials.Any()) return;

            var gap = GapFillerService.Instance.GetCurrentGap();
            if (gap == null) return;

            var (gapStart, gapEnd) = gap.Value;
            _gapQueue = GapFillerService.Instance.BuildQueue(gapStart, gapEnd);
            if (!_gapQueue.Any()) return;

            var now = DateTime.Now;
            _gapIndex = _gapQueue.FindIndex(s => now < s.WallStopTime);
            if (_gapIndex < 0) return;

            _inGap = true;
            IsOverlayVisible = false;
            PlayGapSlot(_gapIndex);
        }

        private void PlayGapSlot(int index)
        {
            if (index >= _gapQueue.Count) { StopGapFiller(); return; }

            var slot = _gapQueue[index];
            if (!File.Exists(slot.Commercial.FilePath)) { AdvanceGapSlot(); return; }

            NowPlaying = $"[Filler] {slot.Commercial.Title}";
            var media = CreateMedia(slot.Commercial.FilePath);
            MediaPlayer.Play(media);
            ArmGapCutTimer(slot.WallStopTime);
        }

        private void ArmGapCutTimer(DateTime wallStopTime)
        {
            _gapCutTimer?.Stop();
            var delay = wallStopTime - DateTime.Now;
            if (delay <= TimeSpan.Zero) { AdvanceGapSlot(); return; }

            _gapCutTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = delay
            };
            _gapCutTimer.Tick += (s, e) => { _gapCutTimer.Stop(); AdvanceGapSlot(); };
            _gapCutTimer.Start();
        }

        private void AdvanceGapSlot()
        {
            if (!_inGap) return;

            var nextShow = SchedulerService.Instance.GetCurrentItem();
            if (nextShow != null)
            {
                StopGapFiller();
                StartShow(nextShow);
                UpdateUpNext();
                return;
            }

            _gapIndex++;
            if (_gapIndex >= _gapQueue.Count)
            {
                var gap = GapFillerService.Instance.GetCurrentGap();
                if (gap != null)
                {
                    var (gs, ge) = gap.Value;
                    _gapQueue = GapFillerService.Instance.BuildQueue(gs, ge);
                    _gapIndex = 0;
                }
                else { StopGapFiller(); return; }
            }
            PlayGapSlot(_gapIndex);
        }

        private void StopGapFiller()
        {
            _gapCutTimer?.Stop();
            _gapCutTimer = null;
            _inGap       = false;
            _gapQueue    = null;
            _gapIndex    = 0;
        }

        // ── EndReached ────────────────────────────────────────────────────────────
        private void OnMediaEndReached(object sender, EventArgs e)
        {
            if (!_playingMidShowCommercial) return;

            var resumeAt  = _savedShowPosition;
            var showPath  = _currentScheduleItem?.FilePath;
            var showTitle = _currentScheduleItem?.EffectiveTitle ?? string.Empty;

            // BeginInvoke from background thread — don't block VLC's thread
            Task.Run(async () =>
            {
                await Task.Delay(500);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (!_playingMidShowCommercial) return;
                    _playingMidShowCommercial = false;
                    IsOverlayVisible          = AppSettings.Instance.OverlayEnabled;

                    if (string.IsNullOrEmpty(showPath) || !File.Exists(showPath)) return;
                    NowPlaying = showTitle;

                    var media = CreateMedia(showPath);
                    MediaPlayer.Play(media);

                    // Seek after VLC starts — fire and forget from background
                    Task.Run(async () =>
                    {
                        await Task.Delay(800);
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            try { MediaPlayer.Time = resumeAt; } catch { }
                        });
                    });
                });
            });
        }

        // ── Core playback ─────────────────────────────────────────────────────────

        /// Creates a Media and disposes the previous one to prevent memory leaks
        private Media CreateMedia(string path)
        {
            var prev = _currentMedia;
            var media = new Media(_libVLC, new Uri(path));

            string sub = Path.ChangeExtension(path, ".srt");
            if (File.Exists(sub)) media.AddOption($":sub-file={sub}");

            _currentMedia = media;

            // Dispose previous after a short delay so VLC has fully released it
            if (prev != null)
                Task.Run(async () => { await Task.Delay(1000); prev.Dispose(); });

            return media;
        }

        public void PlayVideo(string path, TimeSpan seekTo = default)
        {
            if (!File.Exists(path)) { NowPlaying = "[File not found]"; return; }

            var media = CreateMedia(path);
            MediaPlayer.Play(media);

            if (seekTo > TimeSpan.Zero)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(800);
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try { MediaPlayer.Time = (long)seekTo.TotalMilliseconds; } catch { }
                    });
                });
            }
        }

        private void UpdateUpNext()
        {
            var next = SchedulerService.Instance.GetNextItem();
            UpNext = next != null
                ? $"Up next: {next.VideoTitle}  at  {next.StartTime:HH:mm}"
                : string.Empty;
        }

        // ── Controls ─────────────────────────────────────────────────────────────
        public void Pause()  => MediaPlayer?.Pause();
        public void Stop()
        {
            StopBreakWatcher();
            StopGapFiller();
            _currentScheduleItem      = null;
            _playingMidShowCommercial = false;
            _pendingBreaks            = null;
            MediaPlayer?.Stop();
            // Dispose current media on stop
            var m = _currentMedia;
            _currentMedia = null;
            Task.Run(async () => { await Task.Delay(500); m?.Dispose(); });
        }
        public void Resume() => MediaPlayer?.Play();

        public void SetLogo(string path)
        {
            if (string.IsNullOrEmpty(path)) { LogoImage = null; return; }
            if (!File.Exists(path)) return;

            // Load with OnLoad cache so the file handle is released immediately
            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource      = new Uri(path);
            img.CacheOption    = BitmapCacheOption.OnLoad;
            img.CreateOptions  = BitmapCreateOptions.IgnoreImageCache;
            img.EndInit();
            img.Freeze(); // makes it thread-safe and GC-friendly
            LogoImage = img;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
