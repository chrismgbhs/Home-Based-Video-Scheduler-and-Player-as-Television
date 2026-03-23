using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Home_Based_Video_Scheduler_and_Player_as_Television.Services;
using Home_Based_Video_Scheduler_and_Player_as_Television.ViewModels;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Views
{
    public partial class Player : Window
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int x, int y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE     = 0x0002;
        private const uint SWP_NOSIZE     = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;

        private OverlayWindow _overlay;

        public Player()
        {
            InitializeComponent();
            DataContext = PlayerViewModel.Instance;
            Loaded  += Player_Loaded;
            Closing += Player_Closing;
        }

        private void Player_Loaded(object sender, RoutedEventArgs e)
        {
            SchedulerService.Instance.IsPlayerOpen = true;

            WindowState = WindowState.Normal;
            Left   = 0;
            Top    = 0;
            Width  = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;

            // Step 1: attach render surface
            VideoViewControl.MediaPlayer = PlayerViewModel.Instance.MediaPlayer;

            // Step 2: overlay
            _overlay = new OverlayWindow
            {
                DataContext   = PlayerViewModel.Instance,
                ShowInTaskbar = false,
                Left          = Left,
                Top           = Top,
                Width         = Width,
                Height        = Height
            };
            _overlay.SourceInitialized += (s, _) => BringOverlayToFront();
            _overlay.Show();

            LocationChanged += (s, _) => SyncOverlay();
            SizeChanged     += (s, _) => SyncOverlay();
            Activated       += (s, _) => BringOverlayToFront();

            // Step 3: defer playback until AFTER WPF finishes all layout/render passes
            // so VideoView's HWND is fully bound before VLC tries to render into it
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                PlayerViewModel.Instance.OnPlayerWindowReady();
            }));
        }

        private void Player_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SchedulerService.Instance.IsPlayerOpen = false;
            PlayerViewModel.Instance.Stop();
            VideoViewControl.MediaPlayer = null;
            _overlay?.Close();
            _overlay = null;
        }

        private void SyncOverlay()
        {
            if (_overlay == null) return;
            _overlay.Left   = Left;
            _overlay.Top    = Top;
            _overlay.Width  = Width;
            _overlay.Height = Height;
            BringOverlayToFront();
        }

        private void BringOverlayToFront()
        {
            if (_overlay == null) return;
            var hwnd = new WindowInteropHelper(_overlay).Handle;
            if (hwnd == IntPtr.Zero) return;
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }
    }
}
