namespace Home_Based_Video_Scheduler_and_Player_as_Television.Models
{
    public class CommercialModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string FilePath { get; set; }
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Relative play frequency. Default 1.
        /// Weight 3 means this ad plays ~3x as often as a weight-1 ad.
        /// Valid range: 1–10.
        /// </summary>
        public int Weight { get; set; } = 1;
    }
}
