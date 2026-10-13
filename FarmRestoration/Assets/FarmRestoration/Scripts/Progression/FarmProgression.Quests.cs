using System;

namespace FarmRestoration
{
    public sealed partial class FarmProgression
    {
        private FarmQuestProgress quests = new FarmQuestProgress();
        private bool questsLoaded;
        public FarmQuestProgress QuestProgress => quests;
        public bool IsReady => ready;

        public FarmQuestContext QuestContext
        {
            get
            {
                int adults = 0;
                foreach (CowAnimal cow in cows) if (cow != null && cow.IsPenned && !cow.IsCalf) adults++;
                return new FarmQuestContext(orderIndex, adults, IsRepaired(1), IsRepaired(0));
            }
        }

        private void InitializeQuests()
        {
            if (!questsLoaded)
            {
                // Older saves did not track consumed historical items; credit evidenced minimum only.
                quests.RecordCarrots(CarrotCount);
                if (orderIndex >= 2) quests.RecordCarrots(3); // Second original order consumes three carrots.
                quests.talkedToVillager = orderIndex > 0;
                if (products[0] > 0 || products[1] > 0 || products[2] > 0 || orderIndex >= 4) quests.RecordProduction();
                if (milk > 0) quests.RecordMilk();
                for (int i = 0; i < Math.Min(3, fish); i++) quests.RecordFish();
                foreach (CowAnimal cow in cows)
                {
                    if (cow == null) continue;
                    CowSaveData saved = cow.Snapshot();
                    if (saved.milkReadyUtcTicks > 0 || saved.fedUntilUtcTicks > 0) quests.RecordFeed(saved.id);
                }
            }
            foreach (FarmPlot plot in plots) if (plot != null) plot.CropHarvested += RecordQuestHarvest;
        }

        private void RecordQuestHarvest(CropType crop, int count)
        {
            if (!ready || crop != CropType.Carrot || count <= 0) return;
            quests.RecordCarrots(count);
            NotifyAndSave();
        }

        public void RecordQuestTalk()
        {
            if (!ready || quests.talkedToVillager) return;
            quests.talkedToVillager = true;
            NotifyAndSave();
        }

        public bool TryGiveQuestFish()
        {
            if (!ready || quests.claimedStage != 4 || quests.fishDelivered >= 2 || fish < 1) return false;
            fish--;
            quests.fishDelivered++;
            NotifyAndSave();
            return true;
        }

        public bool TryClaimQuest()
        {
            if (!ready || !FarmQuestRules.CanClaim(quests, QuestContext)) return false;
            int reward = FarmQuestRules.Reward(quests.claimedStage);
            if (coins > int.MaxValue - reward) return false;
            quests.claimedStage++;
            coins += reward;
            CowBreedingHud.Show(quests.claimedStage == 6 ? "VILLAGE RESTORED! +40 coins. Thank you!"
                : "Chapter complete! +" + reward + " coins | " + FarmQuestRules.Title(quests.claimedStage));
            NotifyAndSave();
            return true;
        }
    }
}
