using Home_Based_Video_Scheduler_and_Player_as_Television.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Services
{
    public class GapFillerService
    {
        private static GapFillerService _instance;
        public static GapFillerService Instance => _instance ??= new GapFillerService();

        private readonly Random _rng = new Random();

        // ── Repeat-avoidance history ───────────────────────────────────────────────
        // Tracks how many times each commercial has played in the current session.
        // Used to penalise recently over-played ads.
        private readonly Dictionary<int, int> _playCount = new();

        // How many of the most recently played IDs are in the "cooling off" list.
        // A cooled-off commercial has its effective weight halved.
        private const int CooldownWindow = 3;
        private readonly Queue<int> _recentIds = new();   // last N played IDs

        private GapFillerService() { }

        // ── Public slot type ──────────────────────────────────────────────────────
        public class FillerSlot
        {
            public CommercialModel Commercial { get; set; }
            public DateTime WallStartTime    { get; set; }
            public DateTime WallStopTime     { get; set; }
            public TimeSpan AllowedDuration  => WallStopTime - WallStartTime;
        }

        // ── Build queue ───────────────────────────────────────────────────────────
        public List<FillerSlot> BuildQueue(DateTime gapStart, DateTime gapEnd)
        {
            var result      = new List<FillerSlot>();
            var commercials = CommercialStore.Instance.Commercials
                .Where(c => c.Duration > TimeSpan.Zero && File.Exists(c.FilePath))
                .ToList();

            if (!commercials.Any()) return result;

            var cursor = gapStart;

            while (cursor < gapEnd)
            {
                var c    = PickCommercial(commercials);
                var stop = cursor + c.Duration;
                if (stop > gapEnd) stop = gapEnd;

                result.Add(new FillerSlot
                {
                    Commercial    = c,
                    WallStartTime = cursor,
                    WallStopTime  = stop
                });

                RecordPlay(c.Id);
                cursor = stop;
            }

            return result;
        }

        // ── Weighted random picker with repeat avoidance ──────────────────────────

        /// <summary>
        /// Picks a commercial using weighted random selection.
        /// Recently played ads have their effective weight halved (cooldown penalty).
        /// The most recently played ad is fully excluded if there are alternatives.
        /// </summary>
        private CommercialModel PickCommercial(List<CommercialModel> pool)
        {
            if (pool.Count == 1) return pool[0];

            var lastPlayedId  = _recentIds.Count > 0 ? _recentIds.Last() : -1;
            var recentSet     = new HashSet<int>(_recentIds);

            // Build effective weight list
            var weighted = pool.Select(c =>
            {
                // Exclude the immediately last-played ad if there's an alternative
                if (c.Id == lastPlayedId && pool.Count > 1)
                    return (commercial: c, effectiveWeight: 0);

                double w = Math.Max(1, c.Weight);   // base weight (min 1)

                // Halve weight for anything in the cooldown window
                if (recentSet.Contains(c.Id))
                    w *= 0.5;

                // Further reduce weight proportional to how many times it has played
                _playCount.TryGetValue(c.Id, out int plays);
                if (plays > 0)
                    w /= (1 + plays * 0.25);   // each extra play reduces weight by 25%

                return (commercial: c, effectiveWeight: w);
            }).Where(x => x.effectiveWeight > 0).ToList();

            // If all got excluded somehow (shouldn't happen), fall back to full pool
            if (!weighted.Any())
                weighted = pool.Select(c => (commercial: c, effectiveWeight: (double)Math.Max(1, c.Weight))).ToList();

            // Weighted random pick
            double total = weighted.Sum(x => x.effectiveWeight);
            double roll  = _rng.NextDouble() * total;
            double acc   = 0;

            foreach (var (commercial, effectiveWeight) in weighted)
            {
                acc += effectiveWeight;
                if (roll < acc) return commercial;
            }

            return weighted.Last().commercial;  // fallback
        }

        private void RecordPlay(int id)
        {
            // Update play count
            _playCount[id] = _playCount.TryGetValue(id, out var n) ? n + 1 : 1;

            // Update recency queue
            _recentIds.Enqueue(id);
            while (_recentIds.Count > CooldownWindow)
                _recentIds.Dequeue();
        }

        // ── Gap detection ─────────────────────────────────────────────────────────
        public (DateTime gapStart, DateTime gapEnd)? GetCurrentGap()
        {
            var schedule = VideoStore.Instance.Schedule
                .OrderBy(s => s.StartTime)
                .ToList();

            if (!schedule.Any()) return null;

            var now    = DateTime.Now;
            bool inShow = schedule.Any(s => now >= s.StartTime && now < s.EndTime);
            if (inShow) return null;

            var prev = schedule
                .Where(s => s.EndTime <= now)
                .OrderByDescending(s => s.EndTime)
                .FirstOrDefault();

            var next = schedule
                .Where(s => s.StartTime > now)
                .OrderBy(s => s.StartTime)
                .FirstOrDefault();

            if (next == null) return null;

            var gapStart = prev?.EndTime ?? now;
            var gapEnd   = next.StartTime;

            if (gapEnd <= now) return null;

            return (gapStart, gapEnd);
        }

        // ── Reset history (call when app session resets if desired) ───────────────
        public void ResetHistory()
        {
            _playCount.Clear();
            _recentIds.Clear();
        }
    }
}
