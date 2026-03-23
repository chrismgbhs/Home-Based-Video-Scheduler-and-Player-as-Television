using Home_Based_Video_Scheduler_and_Player_as_Television.Services;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.ViewModels
{
    class MainMenuViewModel
    {
        public ICommand ViewPlayerCommand        { get; }
        public ICommand VideoLibraryCommand      { get; }
        public ICommand CommercialLibraryCommand { get; }
        public ICommand CommercialBreakCommand   { get; }
        public ICommand SchedulerCommand         { get; }
        public ICommand SettingsCommand          { get; }

        // Live stats for the right panel
        public int ScheduledCount  => VideoStore.Instance.Schedule.Count;
        public int VideoCount      => VideoStore.Instance.Videos.Count;
        public int CommercialCount => CommercialStore.Instance.Commercials.Count;

        public MainMenuViewModel()
        {
            ViewPlayerCommand        = new RelayCommand(ExecuteViewPlayer);
            VideoLibraryCommand      = new RelayCommand(ExecuteVideoLibrary);
            CommercialLibraryCommand = new RelayCommand(ExecuteCommercialLibrary);
            SchedulerCommand         = new RelayCommand(ExecuteScheduler);
            SettingsCommand          = new RelayCommand(ExecuteSettings);
            CommercialBreakCommand   = new RelayCommand(ExecuteCommercialBreak);
        }
        private void ExecuteViewPlayer() => new Views.Player().Show();

        private void ExecuteVideoLibrary()      => new Views.VideoLibrary().Show();
        private void ExecuteCommercialLibrary() => new Views.CommercialLibrary().Show();
        private void ExecuteScheduler()         => new Views.Schedule().Show();
        private void ExecuteCommercialBreak()   => new Views.CommercialBreakView().Show();

        private void ExecuteSettings() => new Views.Settings().Show();
    }
}
