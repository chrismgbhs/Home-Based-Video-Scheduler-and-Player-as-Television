using System.Windows;
using Home_Based_Video_Scheduler_and_Player_as_Television.ViewModels;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Views
{
    public partial class Player : Window
    {

        private OverlayWindow overlay;

        public Player()
        {
            InitializeComponent();

            var vm = new PlayerViewModel();
            DataContext = vm;

            Loaded += Player_Loaded;
        }

        private void Player_Loaded(object sender, RoutedEventArgs e)
        {
            // Force TRUE fullscreen (no taskbar)
            this.WindowState = WindowState.Normal;
            this.Left = 0;
            this.Top = 0;
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;

            overlay = new OverlayWindow
            {
                DataContext = this.DataContext,
                Owner = this,
                Topmost = true,
                ShowInTaskbar = false
            };

            overlay.Show();

            SyncOverlay();

            this.LocationChanged += (s, _) => SyncOverlay();
            this.SizeChanged += (s, _) => SyncOverlay();
            this.StateChanged += (s, _) => SyncOverlay();
        }

        private void SyncOverlay()
        {
            if (overlay == null) return;

            overlay.Left = this.Left;
            overlay.Top = this.Top;
            overlay.Width = this.Width;
            overlay.Height = this.Height;
        }
    }
}