using System;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Models
{
    public class ScheduleItem
    {
        public int VideoId { get; set; }
        public string VideoTitle { get; set; }
        public string FilePath { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Optional custom on-screen title. If empty, VideoTitle is used instead.
        /// </summary>
        public string DisplayTitle { get; set; }

        /// <summary>Returns DisplayTitle if set, otherwise VideoTitle.</summary>
        public string EffectiveTitle =>
            string.IsNullOrWhiteSpace(DisplayTitle) ? VideoTitle : DisplayTitle;

        public string Duration
        {
            get
            {
                var d = EndTime - StartTime;
                if (d.TotalSeconds <= 0) return "—";
                if (d.TotalHours >= 1)
                    return $"{(int)d.TotalHours}h {d.Minutes}m";
                return $"{d.Minutes}m {d.Seconds}s";
            }
        }
    }
}
