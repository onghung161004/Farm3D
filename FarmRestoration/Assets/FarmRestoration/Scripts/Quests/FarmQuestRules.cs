using System;

namespace FarmRestoration
{
    public static class FarmQuestRules
    {
        public const int StageCount = 6;
        private static readonly string[] Titles = { "A fresh start", "Supply the village", "Restore the bridge",
            "Bring back dairy farming", "Food from the water", "The village comes alive" };
        private static readonly int[] Rewards = { 15, 20, 20, 25, 20, 40 };
        public static string Title(int stage) => stage >= 0 && stage < StageCount ? Titles[stage] : "Village restored";
        public static int Reward(int stage) => stage >= 0 && stage < StageCount ? Rewards[stage] : 0;

        public static bool CanClaim(FarmQuestProgress progress, FarmQuestContext context)
        {
            if (progress == null) return false;
            switch (progress.claimedStage)
            {
                case 0: return progress.talkedToVillager && progress.carrotsHarvested >= 3;
                case 1: return progress.itemsProduced >= 1 && context.OrdersDelivered >= 1;
                case 2: return context.BridgeRestored;
                case 3: return context.AdultCowsPenned >= 2 && progress.fedCowIds != null
                    && progress.fedCowIds.Length >= 2 && progress.milkCollected >= 1;
                case 4: return progress.fishCaught >= 3 && progress.fishDelivered >= 2;
                case 5: return context.HouseRestored && context.BridgeRestored;
                default: return false;
            }
        }

        public static string Objectives(FarmQuestProgress p, FarmQuestContext c)
        {
            if (p == null) return "Quests loading...";
            switch (p.claimedStage)
            {
                case 0: return Goal("Talk to a resident or trader", p.talkedToVillager ? 1 : 0, 1)
                    + "\n" + Goal("Harvest carrots", p.carrotsHarvested, 3);
                case 1: return Goal("Make a processed item", p.itemsProduced, 1) + "\n" + Goal("Deliver a trader order", c.OrdersDelivered, 1);
                case 2: return Goal("Repair the wooden bridge", c.BridgeRestored ? 1 : 0, 1) + "\nUse its repair sign: 45 coins + 10 wood.";
                case 3: return Goal("Adult cows in pen", c.AdultCowsPenned, 2) + "\n" + Goal("Feed different cows", p.fedCowIds?.Length ?? 0, 2)
                    + "\n" + Goal("Collect milk", p.milkCollected, 1);
                case 4: return Goal("Catch fish", p.fishCaught, 3) + "\n" + Goal("Donate fish at this board", p.fishDelivered, 2)
                    + "\nDonations use your warehouse fish.";
                case 5: return Goal("Restore the wooden house", c.HouseRestored ? 1 : 0, 1)
                    + "\nReturn here and claim the final reward.";
                default: return "Thank you for restoring our village!\nThe bridge and house are repaired, and food supplies are back.\nKeep farming, fishing and expanding your farm.";
            }
        }

        private static string Goal(string name, int count, int total)
            => (count >= total ? "[DONE] " : "[ ] ") + name + ": " + Math.Max(0, Math.Min(count, total)) + "/" + total;
    }
}
