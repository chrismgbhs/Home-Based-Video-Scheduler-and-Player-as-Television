using System;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Models
{
    public class ScheduleItem
    {
        public int VideoId { get; set; }          // The ID of the scheduled video
        public DateTime StartTime { get; set; }   // When the video should start
        public DateTime EndTime { get; set; }     // When the video ends

        // Optional helper for DataGrid display
        public string VideoTitle { get; set; }    // You can set this from the VideoModel in the ViewModel
    }
}