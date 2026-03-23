using System.Windows;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Views
{
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
            DataContext = new ViewModels.SettingsViewModel();
        }
    }
}
