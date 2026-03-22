using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.ViewModels
{
    class MainMenuViewModel
    {
        public ICommand ViewPlayerCommand { get; set; }
        public ICommand SchedulerCommand { get; set; }
        public ICommand SettingsCommand { get; set; }

        public MainMenuViewModel()
        {
            ViewPlayerCommand = new RelayCommand(ExecuteViewPlayer);
            SchedulerCommand = new RelayCommand(ExecuteScheduler);
            SettingsCommand = new RelayCommand(ExecuteSettings);
        }

        public void ExecuteViewPlayer()
        {
            var mainWindow = new Views.Player();
            Application.Current.MainWindow = mainWindow; // ✅ Set BEFORE closing
            mainWindow.Show();                           // ✅ Non-blocking
            Application.Current.Windows
                .OfType<Views.MainMenu>()
                .FirstOrDefault()?.Close();                 // ✅ Close login after
        }
        public void ExecuteScheduler()
        {
            var mainWindow = new Views.Scheduler();
            Application.Current.MainWindow = mainWindow; // ✅ Set BEFORE closing
            mainWindow.Show();                           // ✅ Non-blocking
            Application.Current.Windows
                .OfType<Views.MainMenu>()
                .FirstOrDefault()?.Close();                 // ✅ Close login after
        }
        public void ExecuteSettings()
        {
            var mainWindow = new Views.Settings();
            Application.Current.MainWindow = mainWindow; // ✅ Set BEFORE closing
            mainWindow.Show();                           // ✅ Non-blocking
            Application.Current.Windows
                .OfType<Views.MainMenu>()
                .FirstOrDefault()?.Close();                 // ✅ Close login after
        }
    }
}
