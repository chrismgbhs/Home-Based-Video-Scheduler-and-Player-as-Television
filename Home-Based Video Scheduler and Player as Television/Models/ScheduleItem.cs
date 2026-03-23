using Home_Based_Video_Scheduler_and_Player_as_Television.Services;
using System;
using System.Linq;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Models
{
    public class ScheduleItem
    {
        public int VideoId { get; set; }
        public string VideoTitle { get; set; }
        public string FilePath { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string DisplayTitle { get; set; }

        public string EffectiveTitle =>
            string.IsNullOrWhiteSpace(DisplayTitle) ? VideoTitle : DisplayTitle;

        // ── Show duration (video only) ────────────────────────────────────────────
        public string Duration
        {
            get
            {
                var d = EndTime - StartTime;
                if (d.TotalSeconds <= 0) return "—";
                if (d.TotalHours >= 1)
                    return $"{(int)d.TotalHours}h {d.Minutes}m {d.Seconds}s";
                return $"{d.Minutes}m {d.Seconds}s";
            }
        }

        // ── True end time = video end + all commercials ─────────────────────────────
        public string TrueEndTimeDisplay
        {
            get
            {
                var trueEnd = CommercialBreakStore.Instance.GetTrueEndTime(this);
                // If no commercials, true end == video end — show just the time
                if (trueEnd == EndTime)
                    return EndTime.ToString("MM/dd/yyyy  HH:mm:ss");
                return trueEnd.ToString("MM/dd/yyyy  HH:mm:ss");
            }
        }

        // ── Total commercial time for this show ───────────────────────────────────
        public string AdTime
        {
            get
            {
                var breaks = CommercialBreakStore.Instance.Breaks
                    .Where(b => b.ShowFilePath  == FilePath &&
                                b.ShowStartTime == StartTime)
                    .ToList();

                if (!breaks.Any()) return "—";

                // Sum commercial durations from the CommercialStore
                var total = TimeSpan.Zero;
                foreach (var b in breaks)
                {
                    var commercial = CommercialStore.Instance.Commercials
                        .FirstOrDefault(c => c.Id == b.CommercialId);
                    if (commercial != null)
                        total += commercial.Duration;
                }

                if (total == TimeSpan.Zero) return "—";
                if (total.TotalHours >= 1)
                    return $"{(int)total.TotalHours}h {total.Minutes}m {total.Seconds}s";
                return $"{total.Minutes}m {total.Seconds}s";
            }
        }

        // ── Total airtime = show + all commercials ────────────────────────────────
        public string TotalAirTime
        {
            get
            {
                var showDur = EndTime - StartTime;
                if (showDur.TotalSeconds <= 0) return "—";

                var breaks = CommercialBreakStore.Instance.Breaks
                    .Where(b => b.ShowFilePath  == FilePath &&
                                b.ShowStartTime == StartTime)
                    .ToList();

                var adTotal = TimeSpan.Zero;
                foreach (var b in breaks)
                {
                    var commercial = CommercialStore.Instance.Commercials
                        .FirstOrDefault(c => c.Id == b.CommercialId);
                    if (commercial != null)
                        adTotal += commercial.Duration;
                }

                var total = showDur + adTotal;
                if (total.TotalHours >= 1)
                    return $"{(int)total.TotalHours}h {total.Minutes}m {total.Seconds}s";
                return $"{total.Minutes}m {total.Seconds}s";
            }
        }
    }
}
