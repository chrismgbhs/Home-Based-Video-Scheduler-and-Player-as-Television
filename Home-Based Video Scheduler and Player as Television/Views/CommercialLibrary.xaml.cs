using System.Windows;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Views
{
    public partial class CommercialLibrary : Window
    {
        public CommercialLibrary()
        {
            InitializeComponent();
            DataContext = new ViewModels.CommercialLibraryViewModel();
        }
    }
}
