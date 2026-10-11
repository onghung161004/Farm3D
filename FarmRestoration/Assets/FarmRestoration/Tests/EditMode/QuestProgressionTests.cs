using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class QuestProgressionTests
    {
        private GameObject runnerObject;
        private QuestManager questManager;

        [SetUp]
        public void Setup()
        {
            runnerObject = new GameObject("QuestTestRunner");
            questManager = runnerObject.AddComponent<QuestManager>();
        }

        [TearDown]
        public void Teardown()
        {
            if (runnerObject != null)
            {
                Object.DestroyImmediate(runnerObject);
            }
        }

        private QuestData CreateQuest(
            string id,
            string title,
            QuestConditionType type,
            CropType crop = CropType.Pumpkin,
            bool anyCrop = false,
            int amount = 1,
            int repair = 0,
            int coins = 10,
            string unlock = "")
        {
            QuestData quest = ScriptableObject.CreateInstance<QuestData>();
            quest.Configure(id, title, "Test Quest", type, crop, anyCrop, amount, repair, coins, unlock);
            return quest;
        }

        private QuestChapterData CreateChapter(string id, string title, params QuestData[] quests)
        {
            QuestChapterData chapter = ScriptableObject.CreateInstance<QuestChapterData>();
            chapter.Configure(id, title, quests);
            return chapter;
        }

        [Test]
        public void QuestConditions_Plant_AdvancesAndCompletes()
        {
            QuestData quest = CreateQuest("q_plant", "Plant Pumpkins", QuestConditionType.Plant, CropType.Pumpkin, false, 2);
            QuestChapterData chapter = CreateChapter("c1", "Ch 1", quest);
            questManager.Configure(new[] { chapter });

            Assert.That(questManager.GetQuestProgress("q_plant"), Is.EqualTo(0));
            Assert.That(questManager.IsQuestCompleted("q_plant"), Is.False);

            // Plant wrong crop -> should not advance
            questManager.ReportCropPlanted(CropType.Carrot);
            Assert.That(questManager.GetQuestProgress("q_plant"), Is.EqualTo(0));

            // Plant target crop
            questManager.ReportCropPlanted(CropType.Pumpkin);
            Assert.That(questManager.GetQuestProgress("q_plant"), Is.EqualTo(1));
            Assert.That(questManager.IsQuestCompleted("q_plant"), Is.False);

            // Plant second target crop -> completes!
            questManager.ReportCropPlanted(CropType.Pumpkin);
            Assert.That(questManager.GetQuestProgress("q_plant"), Is.EqualTo(2));
            Assert.That(questManager.IsQuestCompleted("q_plant"), Is.True);
        }

        [Test]
        public void QuestConditions_Harvest_AdvancesAndCompletes()
        {
            QuestData quest = CreateQuest("q_harvest", "Harvest Carrots", QuestConditionType.Harvest, CropType.Carrot, false, 3);
            QuestChapterData chapter = CreateChapter("c1", "Ch 1", quest);
            questManager.Configure(new[] { chapter });

            questManager.ReportCropHarvested(CropType.Carrot, 2);
            Assert.That(questManager.GetQuestProgress("q_harvest"), Is.EqualTo(2));
            Assert.That(questManager.IsQuestCompleted("q_harvest"), Is.False);

            questManager.ReportCropHarvested(CropType.Carrot, 1);
            Assert.That(questManager.GetQuestProgress("q_harvest"), Is.EqualTo(3));
            Assert.That(questManager.IsQuestCompleted("q_harvest"), Is.True);
        }

        [Test]
        public void QuestConditions_Process_AdvancesAndCompletes()
        {
            QuestData quest = CreateQuest("q_process", "Make Carrot Juice", QuestConditionType.Process, CropType.Carrot, false, 1);
            QuestChapterData chapter = CreateChapter("c1", "Ch 1", quest);
            questManager.Configure(new[] { chapter });

            questManager.ReportRecipeProcessed(CropType.Carrot, 1);
            Assert.That(questManager.IsQuestCompleted("q_process"), Is.True);
        }

        [Test]
        public void QuestConditions_DeliverOrder_AdvancesAndCompletes()
        {
            QuestData quest = CreateQuest("q_order", "Deliver 2 Orders", QuestConditionType.DeliverOrder, amount: 2);
            QuestChapterData chapter = CreateChapter("c1", "Ch 1", quest);
            questManager.Configure(new[] { chapter });

            questManager.ReportOrderDelivered(1);
            Assert.That(questManager.GetQuestProgress("q_order"), Is.EqualTo(1));
            Assert.That(questManager.IsQuestCompleted("q_order"), Is.False);

            questManager.ReportOrderDelivered(1);
            Assert.That(questManager.IsQuestCompleted("q_order"), Is.True);
        }

        [Test]
        public void QuestConditions_EarnMoney_AdvancesAndCompletes()
        {
            QuestData quest = CreateQuest("q_earn", "Earn 30 Coins", QuestConditionType.EarnMoney, amount: 30);
            QuestChapterData chapter = CreateChapter("c1", "Ch 1", quest);
            questManager.Configure(new[] { chapter });

            questManager.ReportMoneyEarned(12);
            Assert.That(questManager.GetQuestProgress("q_earn"), Is.EqualTo(12));
            Assert.That(questManager.IsQuestCompleted("q_earn"), Is.False);

            questManager.ReportMoneyEarned(20);
            Assert.That(questManager.IsQuestCompleted("q_earn"), Is.True);
        }

        [Test]
        public void QuestConditions_Repair_AdvancesAndCompletes()
        {
            QuestData quest = CreateQuest("q_repair", "Repair Bridge", QuestConditionType.Repair, repair: 1, unlock: "Bridge Unlocked");
            QuestChapterData chapter = CreateChapter("c1", "Ch 1", quest);
            questManager.Configure(new[] { chapter });

            // Repair wrong index
            questManager.ReportRepairCompleted(0);
            Assert.That(questManager.IsQuestCompleted("q_repair"), Is.False);

            // Repair target index
            questManager.ReportRepairCompleted(1);
            Assert.That(questManager.IsQuestCompleted("q_repair"), Is.True);
            Assert.That(questManager.IsRewardUnlocked("Bridge Unlocked"), Is.True);
        }

        [Test]
        public void ChapterProgression_CompletingAllQuestsUnlocksNextChapter()
        {
            QuestData q1 = CreateQuest("q1", "Plant 1", QuestConditionType.Plant, CropType.Pumpkin, false, 1);
            QuestData q2 = CreateQuest("q2", "Harvest 1", QuestConditionType.Harvest, CropType.Pumpkin, false, 1);
            QuestChapterData ch1 = CreateChapter("ch1", "Chapter 1", q1, q2);

            QuestData q3 = CreateQuest("q3", "Deliver Order", QuestConditionType.DeliverOrder, amount: 1);
            QuestChapterData ch2 = CreateChapter("ch2", "Chapter 2", q3);

            questManager.Configure(new[] { ch1, ch2 });

            Assert.That(questManager.CurrentChapterIndex, Is.EqualTo(0));
            Assert.That(questManager.CurrentChapter.ChapterId, Is.EqualTo("ch1"));

            // Complete first quest of chapter 1
            questManager.ReportCropPlanted(CropType.Pumpkin);
            Assert.That(questManager.IsQuestCompleted("q1"), Is.True);
            Assert.That(questManager.CurrentChapterIndex, Is.EqualTo(0)); // Still ch1

            // Complete second quest of chapter 1 -> triggers next chapter!
            questManager.ReportCropHarvested(CropType.Pumpkin, 1);
            Assert.That(questManager.IsQuestCompleted("q2"), Is.True);
            Assert.That(questManager.CurrentChapterIndex, Is.EqualTo(1));
            Assert.That(questManager.CurrentChapter.ChapterId, Is.EqualTo("ch2"));

            // Complete chapter 2
            questManager.ReportOrderDelivered(1);
            Assert.That(questManager.IsQuestCompleted("q3"), Is.True);
            Assert.That(questManager.IsAllChaptersCompleted, Is.True);
        }

        [Test]
        public void SaveLoadRoundTrip_PreservesChapterIndexAndQuestProgress()
        {
            QuestData q1 = CreateQuest("q1", "Plant", QuestConditionType.Plant, CropType.Pumpkin, false, 5);
            QuestChapterData ch1 = CreateChapter("ch1", "Chapter 1", q1);
            questManager.Configure(new[] { ch1 });

            questManager.ReportCropPlanted(CropType.Pumpkin);
            questManager.ReportCropPlanted(CropType.Pumpkin);
            Assert.That(questManager.GetQuestProgress("q1"), Is.EqualTo(2));

            // Export to save data
            QuestSaveData save = questManager.ExportSaveData();
            string json = JsonUtility.ToJson(save);

            // Restore in fresh manager
            QuestSaveData restored = JsonUtility.FromJson<QuestSaveData>(json);
            questManager.RestoreSaveData(restored);

            Assert.That(questManager.CurrentChapterIndex, Is.EqualTo(0));
            Assert.That(questManager.GetQuestProgress("q1"), Is.EqualTo(2));
            Assert.That(questManager.IsQuestCompleted("q1"), Is.False);
        }

        [Test]
        public void OldSaveCompatibility_NullQuestDataLoadsCleanlyWithoutErrors()
        {
            QuestData q1 = CreateQuest("q1", "Plant", QuestConditionType.Plant, CropType.Pumpkin, false, 2);
            QuestChapterData ch1 = CreateChapter("ch1", "Chapter 1", q1);
            questManager.Configure(new[] { ch1 });

            // Simulate loading an old save where questData was null
            FarmSaveData oldSave = new FarmSaveData
            {
                version = 1,
                coins = 50,
                crops = new[] { 2, 3, 1 },
                questData = null
            };

            Assert.DoesNotThrow(() =>
            {
                questManager.RestoreSaveData(oldSave.questData);
            });

            Assert.That(questManager.CurrentChapterIndex, Is.EqualTo(0));
            Assert.That(questManager.GetQuestProgress("q1"), Is.EqualTo(0));
            Assert.That(questManager.IsQuestCompleted("q1"), Is.False);
        }

        [Test]
        public void QuestTrackerSummary_ProducesCorrectTextForActiveQuests()
        {
            QuestData q1 = CreateQuest("q1", "Plant Pumpkins", QuestConditionType.Plant, CropType.Pumpkin, false, 2);
            QuestChapterData ch1 = CreateChapter("ch1", "Chapter 1: The Beginning", q1);
            questManager.Configure(new[] { ch1 });

            string summary = questManager.GetTrackerSummaryText();
            Assert.That(summary, Does.Contain("[Chapter 1: The Beginning]"));
            Assert.That(summary, Does.Contain("Plant Pumpkins: Plant 2 Pumpkin (0/2)"));

            questManager.ReportCropPlanted(CropType.Pumpkin);
            string summary2 = questManager.GetTrackerSummaryText();
            Assert.That(summary2, Does.Contain("Plant Pumpkins: Plant 2 Pumpkin (1/2)"));

            questManager.ReportCropPlanted(CropType.Pumpkin);
            string summary3 = questManager.GetTrackerSummaryText();
            Assert.That(summary3, Does.Contain("ALL CHAPTERS COMPLETED"));
        }
    }
}
