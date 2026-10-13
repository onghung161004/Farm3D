using System;
using System.Collections.Generic;

namespace FarmRestoration
{
    [Serializable]
    public sealed class FarmQuestProgress
    {
        public int claimedStage;
        public bool talkedToVillager;
        public int carrotsHarvested;
        public int itemsProduced;
        public int milkCollected;
        public int fishCaught;
        public int fishDelivered;
        public string[] fedCowIds = Array.Empty<string>();

        public void RecordCarrots(int count) { if (count > 0) carrotsHarvested = (int)Math.Min(3L, (long)carrotsHarvested + count); }
        public void RecordProduction() => itemsProduced = 1;
        public void RecordMilk() => milkCollected = 1;
        public void RecordFish() => fishCaught = Math.Min(3, fishCaught + 1);
        public void RecordFeed(string cowId)
        {
            if (string.IsNullOrWhiteSpace(cowId) || cowId.Length > 256 || fedCowIds.Length >= 2) return;
            foreach (string id in fedCowIds) if (id == cowId) return;
            var ids = new List<string>(fedCowIds) { cowId };
            fedCowIds = ids.ToArray();
        }

        public FarmQuestProgress Snapshot() => Restore(this);

        public static FarmQuestProgress Restore(FarmQuestProgress saved)
        {
            if (saved == null) return new FarmQuestProgress();
            var result = new FarmQuestProgress
            {
                claimedStage = Math.Max(0, Math.Min(6, saved.claimedStage)),
                talkedToVillager = saved.talkedToVillager,
                carrotsHarvested = Math.Max(0, Math.Min(3, saved.carrotsHarvested)),
                itemsProduced = Math.Max(0, Math.Min(1, saved.itemsProduced)),
                milkCollected = Math.Max(0, Math.Min(1, saved.milkCollected)),
                fishCaught = Math.Max(0, Math.Min(3, saved.fishCaught)),
                fishDelivered = Math.Max(0, Math.Min(2, saved.fishDelivered))
            };
            if (saved.fedCowIds != null)
                foreach (string id in saved.fedCowIds) { result.RecordFeed(id); if (result.fedCowIds.Length == 2) break; }
            return result;
        }
    }

    public readonly struct FarmQuestContext
    {
        public readonly int OrdersDelivered, AdultCowsPenned;
        public readonly bool BridgeRestored, HouseRestored;
        public FarmQuestContext(int orders, int cows, bool bridge, bool house)
        { OrdersDelivered = orders; AdultCowsPenned = cows; BridgeRestored = bridge; HouseRestored = house; }
    }
}
