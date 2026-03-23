using Home_Based_Video_Scheduler_and_Player_as_Television.Models;
using Home_Based_Video_Scheduler_and_Player_as_Television.Services;
using Microsoft.Win32;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.ViewModels
{
    public class CommercialLibraryViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<CommercialModel> Commercials
            => CommercialStore.Instance.Commercials;

        private CommercialModel _selectedCommercial;
        public CommercialModel SelectedCommercial
        {
            get => _selectedCommercial;
            set { _selectedCommercial = value; OnPropertyChanged(nameof(SelectedCommercial)); }
        }

        private string _statusText = string.Empty;
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(nameof(StatusText)); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); }
        }

        public ICommand AddFilesCommand         => new RelayCommand(AddFiles);
        public ICommand AddFolderCommand        => new RelayCommand(AddFolder);
        public ICommand RemoveCommercialCommand => new RelayCommand(RemoveCommercial);
        public ICommand SaveCommand             => new RelayCommand(SaveWeights);

        private static readonly string VideoFilter =
            "Video Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.ts;*.m4v";

        private static readonly string[] VideoExtensions =
            { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".ts", ".m4v" };

        private void AddFiles()
        {
            var dialog = new OpenFileDialog
            {
                Filter      = VideoFilter,
                Title       = "Select Commercial Files",
                Multiselect = true
            };
            if (dialog.ShowDialog() != true) return;
            AddPaths(dialog.FileNames);
        }

        private void AddFolder()
        {
            // Pure WPF folder picker — user navigates to the folder and clicks Open
            var dialog = new OpenFileDialog
            {
                Title           = "Select a folder — click Open while inside it",
                Filter          = "All Files|*.*",
                CheckFileExists = false,
                FileName        = "Select folder"
            };
            if (dialog.ShowDialog() != true) return;

            string folder = Path.GetDirectoryName(dialog.FileName);
            if (!Directory.Exists(folder)) return;

            var files = Directory
                .EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                .Where(f => VideoExtensions.Contains(
                    Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f)
                .ToArray();

            if (files.Length == 0)
            {
                MessageBox.Show("No video files found in the selected folder.",
                    "No Videos", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AddPaths(files);
        }

        private async void AddPaths(string[] paths)
        {
            var existing = new HashSet<string>(
                Commercials.Select(c => c.FilePath),
                StringComparer.OrdinalIgnoreCase);

            var newPaths = paths.Where(p => !existing.Contains(p)).ToArray();

            if (newPaths.Length == 0)
            {
                StatusText = "All selected files are already in the library.";
                return;
            }

            IsBusy     = true;
            StatusText = $"Reading {newPaths.Length} file(s)…";

            var durations = await Task.Run(() =>
                MediaDurationReader.ReadBatch(newPaths));

            foreach (var path in newPaths)
            {
                Commercials.Add(new CommercialModel
                {
                    Id       = CommercialStore.Instance.NextId(),
                    Title    = Path.GetFileNameWithoutExtension(path),
                    FilePath = path,
                    Duration = durations.TryGetValue(path, out var d) ? d : TimeSpan.Zero,
                    Weight   = 1
                });
            }

            CommercialStore.Instance.Save();
            IsBusy     = false;
            StatusText = $"Added {newPaths.Length} commercial(s). Total: {Commercials.Count}";
        }

        private void RemoveCommercial()
        {
            if (SelectedCommercial == null) return;
            Commercials.Remove(SelectedCommercial);
            SelectedCommercial = null;
            CommercialStore.Instance.Save();
            StatusText = $"Removed. Total: {Commercials.Count}";
        }

        private void SaveWeights()
        {
            foreach (var c in Commercials)
                c.Weight = Math.Max(1, Math.Min(10, c.Weight));
            CommercialStore.Instance.Save();
            StatusText = "Weights saved.";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
