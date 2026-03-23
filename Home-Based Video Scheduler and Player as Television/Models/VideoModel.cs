namespace Home_Based_Video_Scheduler_and_Player_as_Television.Models
{
    public class VideoModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string FilePath { get; set; }
        public TimeSpan Duration { get; set; }
    }
}