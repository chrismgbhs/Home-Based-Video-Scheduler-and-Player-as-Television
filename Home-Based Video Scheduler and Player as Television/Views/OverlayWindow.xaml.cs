using Home_Based_Video_Scheduler_and_Player_as_Television.Services;
using Home_Based_Video_Scheduler_and_Player_as_Television.ViewModels;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Views
{
    public partial class OverlayWindow : Window
    {
        private const int GWL_EXSTYLE       = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_NOACTIVATE  = 0x08000000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private PlayerViewModel _vm;
        private string _lastNowPlaying  = string.Empty;
        private string _lastUpNext      = string.Empty;
        private bool   _upNextVisible   = false;
        private DispatcherTimer _upNextTimer;   // shows Up Next a few seconds after title appears
        private DispatcherTimer _upNextHideTimer;

        public OverlayWindow()
        {
            InitializeComponent();
            SourceInitialized += OnSourceInitialized;
            Loaded += OnLoaded;
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            var hwnd  = new WindowInteropHelper(this).Handle;
            int style = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _vm = DataContext as PlayerViewModel;
            if (_vm == null) return;

            _vm.PropertyChanged += OnVmPropertyChanged;
            AppSettings.Instance.PropertyChanged += OnSettingsChanged;

            // Trigger initial state if already playing
            if (!string.IsNullOrEmpty(_vm.NowPlaying))
                AnimateTitleIn(_vm.NowPlaying);
            if (!string.IsNullOrEmpty(_vm.UpNext))
                ScheduleUpNext(_vm.UpNext);
            if (_vm.LogoImage != null)
                AnimateLogoIn();
        }

        private void OnVmPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(PlayerViewModel.NowPlaying):
                    if (_vm.NowPlaying != _lastNowPlaying)
                    {
                        _lastNowPlaying = _vm.NowPlaying;
                        AnimateTitleIn(_vm.NowPlaying);
                        AnimateLogoIn();
                        ScheduleUpNext(_vm.UpNext);
                    }
                    break;

                case nameof(PlayerViewModel.UpNext):
                    if (_vm.UpNext != _lastUpNext)
                    {
                        _lastUpNext = _vm.UpNext;
                        if (_upNextVisible)
                            RefreshUpNext();
                    }
                    break;

                case nameof(PlayerViewModel.LogoImage):
                    if (_vm.LogoImage != null)
                        AnimateLogoIn();
                    break;
            }
        }

        // ── Title: slide in from left ─────────────────────────────────────────────
        private void AnimateTitleIn(string title)
        {
            if (string.IsNullOrEmpty(title)) return;
            var sb = (Storyboard)Resources["TitleSlideIn"];
            TitlePanel.BeginStoryboard(sb);
        }

        // ── Logo: set size and opacity from AppSettings, stays visible ─────────────
        private void AnimateLogoIn()
        {
            if (_vm?.LogoImage == null) return;
            var s = AppSettings.Instance;
            LogoImage.Height  = s.LogoSize;
            LogoImage.Opacity = s.LogoOpacity;
        }

        // ── Up Next: show 3s after title appears, hide after 6s ──────────────────
        private void ScheduleUpNext(string text)
        {
            _upNextTimer?.Stop();
            _upNextHideTimer?.Stop();
            _upNextVisible = false;

            if (string.IsNullOrEmpty(text)) return;

            _upNextTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(3) };
            _upNextTimer.Tick += (s, e) =>
            {
                _upNextTimer.Stop();
                ShowUpNext();
            };
            _upNextTimer.Start();
        }

        private void ShowUpNext()
        {
            _upNextVisible = true;
            var sb = (Storyboard)Resources["UpNextSlideIn"];
            UpNextPanel.BeginStoryboard(sb);

            // Auto-hide after 6s
            _upNextHideTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(6) };
            _upNextHideTimer.Tick += (s, e) =>
            {
                _upNextHideTimer.Stop();
                HideUpNext();
            };
            _upNextHideTimer.Start();
        }

        private void HideUpNext()
        {
            _upNextVisible = false;
            var sb = (Storyboard)Resources["UpNextFadeOut"];
            UpNextPanel.BeginStoryboard(sb);
        }

        private void RefreshUpNext()
        {
            // If already visible, briefly fade out then back in with new text
            HideUpNext();
            if (!string.IsNullOrEmpty(_vm.UpNext))
                ScheduleUpNext(_vm.UpNext);
        }

        private void OnSettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppSettings.LogoSize) ||
                e.PropertyName == nameof(AppSettings.LogoOpacity))
            {
                if (_vm?.LogoImage != null)
                {
                    LogoImage.Height  = AppSettings.Instance.LogoSize;
                    LogoImage.Opacity = AppSettings.Instance.LogoOpacity;
                }
            }
            if (e.PropertyName == nameof(AppSettings.OverlayEnabled))
            {
                // Reflect overlay toggle immediately
                var grid = (System.Windows.Controls.Grid)Content;
                grid.Visibility = AppSettings.Instance.OverlayEnabled
                    ? System.Windows.Visibility.Visible
                    : System.Windows.Visibility.Collapsed;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _upNextTimer?.Stop();
            _upNextHideTimer?.Stop();
            if (_vm != null) _vm.PropertyChanged -= OnVmPropertyChanged;
            AppSettings.Instance.PropertyChanged -= OnSettingsChanged;
            base.OnClosed(e);
        }
    }
}
