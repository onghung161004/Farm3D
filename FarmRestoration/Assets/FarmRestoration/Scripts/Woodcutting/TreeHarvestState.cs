using System;
using UnityEngine;

namespace FarmRestoration
{
    public sealed class TreeHarvestState
    {
        public const int RequiredHits = 3;
        public const int WoodYield = 3;
        public const float RegrowthSeconds = 300f;
        public int Hits { get; private set; }
        public long RegrowUtcTicks { get; private set; }
        public bool IsStump => RegrowUtcTicks > 0;
        public int HitsRemaining => IsStump ? 0 : RequiredHits - Hits;

        public bool TryHit(DateTime now, out bool felled)
        {
            felled = false;
            Tick(now);
            if (IsStump) return false;
            Hits++;
            if (Hits == RequiredHits)
            {
                felled = true;
                RegrowUtcTicks = now.AddSeconds(RegrowthSeconds).Ticks;
            }
            return true;
        }

        public bool Tick(DateTime now)
        {
            if (!IsStump || now.Ticks < RegrowUtcTicks) return false;
            Hits = 0;
            RegrowUtcTicks = 0;
            return true;
        }

        public void Restore(TreeSaveData saved, DateTime now)
        {
            if (saved == null) return;
            RegrowUtcTicks = Math.Max(0, saved.regrowUtcTicks);
            Hits = IsStump ? RequiredHits : Mathf.Clamp(saved.hits, 0, RequiredHits - 1);
            Tick(now);
        }

        public TreeSaveData Snapshot(string id) => new TreeSaveData
        {
            id = id, hits = Hits, regrowUtcTicks = RegrowUtcTicks
        };
    }
}
