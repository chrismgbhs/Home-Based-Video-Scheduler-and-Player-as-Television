using System.Windows;
using Home_Based_Video_Scheduler_and_Player_as_Television.ViewModels;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Views
{
    public partial class Player : Window
    {
        public Player()
        {
            InitializeComponent();
            DataContext = new PlayerViewModel();
        }
    }
}