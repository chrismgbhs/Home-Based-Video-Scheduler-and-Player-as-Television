using Home_Based_Video_Scheduler_and_Player_as_Television.Services;
using Microsoft.Win32;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly AppSettings _s = AppSettings.Instance;

        // ── Logo path ─────────────────────────────────────────────────────────────
        public string LogoPath
        {
            get => _s.LogoPath;
            set
            {
                _s.LogoPath = value;
                OnPropertyChanged(nameof(LogoPath));
                OnPropertyChanged(nameof(LogoPreviewPath));
                PlayerViewModel.Instance.SetLogo(value);
            }
        }

        public string LogoPreviewPath =>
            File.Exists(_s.LogoPath) ? _s.LogoPath : null;

        // ── Logo size ─────────────────────────────────────────────────────────────
        public double LogoSize
        {
            get => _s.LogoSize;
            set { _s.LogoSize = value; OnPropertyChanged(nameof(LogoSize)); OnPropertyChanged(nameof(LogoSizeLabel)); }
        }
        public string LogoSizeLabel => $"{(int)_s.LogoSize} px";

        // ── Logo opacity ──────────────────────────────────────────────────────────
        public double LogoOpacity
        {
            get => _s.LogoOpacity;
            set { _s.LogoOpacity = value; OnPropertyChanged(nameof(LogoOpacity)); OnPropertyChanged(nameof(LogoOpacityLabel)); }
        }
        public string LogoOpacityLabel => $"{(int)(_s.LogoOpacity * 100)} %";

        // ── Feature toggles ───────────────────────────────────────────────────────
        public bool OverlayEnabled
        {
            get => _s.OverlayEnabled;
            set { _s.OverlayEnabled = value; OnPropertyChanged(nameof(OverlayEnabled)); }
        }

        public bool AutoCommercialsEnabled
        {
            get => _s.AutoCommercialsEnabled;
            set { _s.AutoCommercialsEnabled = value; OnPropertyChanged(nameof(AutoCommercialsEnabled)); }
        }

        // ── Commands ──────────────────────────────────────────────────────────────
        public ICommand BrowseLogoCommand => new RelayCommand(BrowseLogo);
        public ICommand ClearLogoCommand  => new RelayCommand(ClearLogo);
        public ICommand SaveCommand       => new RelayCommand(Save);

        private void BrowseLogo()
        {
            var dialog = new OpenFileDialog
            {
                Title  = "Select Channel Logo",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.ico"
            };
            if (dialog.ShowDialog() == true)
                LogoPath = dialog.FileName;
        }

        private void ClearLogo()
        {
            LogoPath = string.Empty;
        }

        private void Save()
        {
            _s.Save();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
