using System;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Models
{
    /// <summary>
    /// A commercial break assigned to a specific scheduled show.
    /// Fires when the show's playback reaches Offset from its start.
    /// </summary>
    public class CommercialBreak
    {
        public int Id { get; set; }

        // Which schedule entry this break belongs to (matched by FilePath + StartTime)
        public string ShowFilePath { get; set; }
        public DateTime ShowStartTime { get; set; }

        // Which commercial to play
        public int CommercialId { get; set; }
        public string CommercialTitle { get; set; }
        public string CommercialFilePath { get; set; }

        // How far into the show to insert the break (e.g. 00:15:00)
        public TimeSpan Offset { get; set; }

        // Runtime flag — true once fired this session so it doesn't repeat
        public bool HasFired { get; set; } = false;

        // Display helper
        public string OffsetDisplay => Offset.ToString(@"hh\:mm\:ss");
    }
}
