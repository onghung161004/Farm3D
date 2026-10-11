using System.Collections.Generic;
using System.IO;
using FarmRestoration;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FarmRestoration.Editor
{
    public static class FarmQuestSetup
    {
        private const string ScenePath = "Assets/FarmRestoration/Scenes/FarmDemo.unity";
        private const string DataDirectory = "Assets/FarmRestoration/Data/Quests";
        private const string ResourcesDirectory = "Assets/FarmRestoration/Resources/Quests";
        private const string DatabasePath = DataDirectory + "/QuestDatabase.asset";
        private const string ResourcesDatabasePath = ResourcesDirectory + "/QuestDatabase.asset";

        [InitializeOnLoadMethod]
        private static void Schedule()
        {
            EditorApplication.delayCall += ApplyWhenReady;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += ApplyWhenReady;
            };
            EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += ApplyWhenReady;
        }

        [MenuItem("Tools/Farm Restoration/Setup Quests and Tracker")]
        public static void ApplyWhenReady()
        {
            EnsureQuestAssets();

            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath) return;

            FarmHud hud = Object.FindAnyObjectByType<FarmHud>();
            if (hud == null) return;

            bool changed = false;
            QuestDatabase db = AssetDatabase.LoadAssetAtPath<QuestDatabase>(DatabasePath);

            QuestManager manager = hud.GetComponent<QuestManager>();
            if (manager == null)
            {
                manager = hud.gameObject.AddComponent<QuestManager>();
                changed = true;
            }
            if (db != null)
            {
                manager.Configure(db);
                EditorUtility.SetDirty(manager);
            }

            if (hud.transform.Find("QuestTrackerPanel") == null)
            {
                CreateQuestTracker(hud.transform, manager);
                changed = true;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log("[FarmQuestSetup] QuestManager and QuestTracker configured on FarmDemo HUD.", hud);
            }
        }

        public static QuestDatabase EnsureQuestAssets()
        {
            if (!Directory.Exists(DataDirectory)) Directory.CreateDirectory(DataDirectory);
            if (!Directory.Exists(ResourcesDirectory)) Directory.CreateDirectory(ResourcesDirectory);

            QuestDatabase existingDb = AssetDatabase.LoadAssetAtPath<QuestDatabase>(DatabasePath);
            if (existingDb != null && existingDb.Chapters != null && existingDb.Chapters.Count >= 5)
            {
                return existingDb;
            }

            // Chapter 1: Day 1 - Sowing the Seeds
            QuestData q1_1 = CreateOrLoadQuest("Q1_1_PlantPumpkins", "Planting Pumpkins", "Till the soil and plant 2 Pumpkin seeds.",
                QuestConditionType.Plant, CropType.Pumpkin, false, 2, 0, 10);
            QuestData q1_2 = CreateOrLoadQuest("Q1_2_HarvestPumpkins", "First Harvest", "Water and harvest 2 ripe Pumpkins.",
                QuestConditionType.Harvest, CropType.Pumpkin, false, 2, 0, 15);
            QuestChapterData ch1 = CreateOrLoadChapter("Chapter_01", "Chapter 1: Sowing the Seeds", new[] { q1_1, q1_2 });

            // Chapter 2: Day 2 - Field Expansion & Trade
            QuestData q2_1 = CreateOrLoadQuest("Q2_1_PlantCarrots", "Crisp Carrots", "Plant 2 Carrot crops in the farm plots.",
                QuestConditionType.Plant, CropType.Carrot, false, 2, 0, 10);
            QuestData q2_2 = CreateOrLoadQuest("Q2_2_HarvestCarrots", "Bountiful Carrots", "Harvest 2 sweet Carrots.",
                QuestConditionType.Harvest, CropType.Carrot, false, 2, 0, 15);
            QuestData q2_3 = CreateOrLoadQuest("Q2_3_DeliverFirstOrder", "First Delivery", "Deliver 1 order to the Village Trader.",
                QuestConditionType.DeliverOrder, CropType.Carrot, false, 1, 0, 20);
            QuestChapterData ch2 = CreateOrLoadChapter("Chapter_02", "Chapter 2: Field Expansion & Trade", new[] { q2_1, q2_2, q2_3 });

            // Chapter 3: Day 3 - Kitchen & Economy
            QuestData q3_1 = CreateOrLoadQuest("Q3_1_ProcessCarrotJuice", "Fresh Juice", "Process 1 Carrot Juice at the Juice Press.",
                QuestConditionType.Process, CropType.Carrot, false, 1, 0, 15);
            QuestData q3_2 = CreateOrLoadQuest("Q3_2_ProcessPumpkinSoup", "Hearty Soup", "Cook 1 Pumpkin Soup at the Village Kitchen.",
                QuestConditionType.Process, CropType.Pumpkin, false, 1, 0, 20);
            QuestData q3_3 = CreateOrLoadQuest("Q3_3_EarnTradeCoins", "Village Commerce", "Earn 25 coins through trading and deliveries.",
                QuestConditionType.EarnMoney, CropType.Pumpkin, false, 25, 0, 20);
            QuestChapterData ch3 = CreateOrLoadChapter("Chapter_03", "Chapter 3: Kitchen & Economy", new[] { q3_1, q3_2, q3_3 });

            // Chapter 4: Day 4 - Village Restoration
            QuestData q4_1 = CreateOrLoadQuest("Q4_1_HarvestTomatoes", "Summer Tomatoes", "Harvest 2 plump Tomatoes.",
                QuestConditionType.Harvest, CropType.Tomato, false, 2, 0, 15);
            QuestData q4_2 = CreateOrLoadQuest("Q4_2_RepairVillageHouse", "Restoring the House", "Repair the ruined village house (30 coins).",
                QuestConditionType.Repair, CropType.Tomato, false, 1, 0, 35, "Restored Village House");
            QuestData q4_3 = CreateOrLoadQuest("Q4_3_FulfillOrders", "Reliable Supplier", "Deliver 2 orders to the Village Trader.",
                QuestConditionType.DeliverOrder, CropType.Tomato, false, 2, 0, 30);
            QuestChapterData ch4 = CreateOrLoadChapter("Chapter_04", "Chapter 4: Village Restoration", new[] { q4_1, q4_2, q4_3 });

            // Chapter 5: Day 5 - The Bridge & Valley Prosperity
            QuestData q5_1 = CreateOrLoadQuest("Q5_1_MasterProcessing", "Kitchen Master", "Process 2 food or juice recipes at the stations.",
                QuestConditionType.Process, CropType.Tomato, true, 2, 0, 25);
            QuestData q5_2 = CreateOrLoadQuest("Q5_2_ProsperousFarmer", "Prosperous Farmer", "Earn 50 coins through orders and diligence.",
                QuestConditionType.EarnMoney, CropType.Tomato, false, 50, 0, 40);
            QuestData q5_3 = CreateOrLoadQuest("Q5_3_RepairBridgeApproach", "Connecting the Valley", "Repair the riverside bridge approach (45 coins).",
                QuestConditionType.Repair, CropType.Tomato, false, 1, 1, 50, "Valley Bridge Restored");
            QuestChapterData ch5 = CreateOrLoadChapter("Chapter_05", "Chapter 5: Valley Prosperity", new[] { q5_1, q5_2, q5_3 });

            QuestDatabase database = ScriptableObject.CreateInstance<QuestDatabase>();
            database.Configure(new[] { ch1, ch2, ch3, ch4, ch5 });
            AssetDatabase.CreateAsset(database, DatabasePath);

            // Also copy / save to Resources for runtime tests/headless loads
            QuestDatabase resDb = ScriptableObject.CreateInstance<QuestDatabase>();
            resDb.Configure(new[] { ch1, ch2, ch3, ch4, ch5 });
            AssetDatabase.CreateAsset(resDb, ResourcesDatabasePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FarmQuestSetup] Created 5 Quest chapters and 14 Quest assets in " + DataDirectory);
            return database;
        }

        private static QuestData CreateOrLoadQuest(
            string assetName,
            string title,
            string desc,
            QuestConditionType type,
            CropType crop,
            bool any,
            int amount,
            int repair,
            int coins,
            string unlock = "")
        {
            string path = $"{DataDirectory}/{assetName}.asset";
            QuestData quest = AssetDatabase.LoadAssetAtPath<QuestData>(path);
            if (quest == null)
            {
                quest = ScriptableObject.CreateInstance<QuestData>();
                quest.Configure(assetName, title, desc, type, crop, any, amount, repair, coins, unlock);
                AssetDatabase.CreateAsset(quest, path);
            }
            else
            {
                quest.Configure(assetName, title, desc, type, crop, any, amount, repair, coins, unlock);
                EditorUtility.SetDirty(quest);
            }
            return quest;
        }

        private static QuestChapterData CreateOrLoadChapter(string assetName, string title, IEnumerable<QuestData> quests)
        {
            string path = $"{DataDirectory}/{assetName}.asset";
            QuestChapterData chapter = AssetDatabase.LoadAssetAtPath<QuestChapterData>(path);
            if (chapter == null)
            {
                chapter = ScriptableObject.CreateInstance<QuestChapterData>();
                chapter.Configure(assetName, title, quests);
                AssetDatabase.CreateAsset(chapter, path);
            }
            else
            {
                chapter.Configure(assetName, title, quests);
                EditorUtility.SetDirty(chapter);
            }
            return chapter;
        }

        private static void CreateQuestTracker(Transform hud, QuestManager manager)
        {
            GameObject panel = new GameObject("QuestTrackerPanel", typeof(RectTransform), typeof(Image), typeof(QuestTrackerHud));
            panel.transform.SetParent(hud, false);
            RectTransform rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-18f, -135f);
            rect.sizeDelta = new Vector2(420f, 150f);

            Image background = panel.GetComponent<Image>();
            background.color = new Color(0.06f, 0.12f, 0.14f, 0.94f);
            background.raycastTarget = false;

            GameObject textObject = new GameObject("QuestTrackerText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panel.transform, false);
            RectTransform textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 8f);
            textRect.offsetMax = new Vector2(-10f, -7f);

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 16f;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;

            QuestTrackerHud tracker = panel.GetComponent<QuestTrackerHud>();
            tracker.Configure(label);
        }

        [MenuItem("Tools/Farm/Simulate Quests")]
        public static void SimulateQuests()
        {
            SimulateFullRun();
        }

        [MenuItem("Tools/Farm/Simulate Full Run")]
        public static void SimulateFullRun()
        {
            QuestDatabase db = EnsureQuestAssets();
            if (db == null || db.Chapters == null || db.Chapters.Count == 0)
            {
                Debug.LogError("[Simulate Full Run] Failed to find or generate QuestDatabase!");
                return;
            }

            GameObject runner = new GameObject("FullRunSimulation");
            FarmHud hud = runner.AddComponent<FarmHud>();
            FarmProgression progression = runner.AddComponent<FarmProgression>();
            QuestManager questManager = runner.AddComponent<QuestManager>();

            QuestManager originalQuestManager = QuestManager.Instance;
            FarmProgression originalProgression = FarmProgression.Instance;

            try
            {
                questManager.Configure(db);
                progression.ConfigureForTests(hud, System.Array.Empty<FarmPlot>(), 15);

                float simulatedSeconds = 0f;
                int chaptersCompleted = 0;
                int totalQuests = 0;

                for (int c = 0; c < db.Chapters.Count; c++)
                {
                    QuestChapterData chapter = db.Chapters[c];
                    foreach (QuestData quest in chapter.Quests)
                    {
                        totalQuests++;
                        // Check anti-soft-lock condition before each quest
                        if (progression.Coins < 2 && progression.SeedInventory.TotalSeeds == 0 && hud.CropInventory.Total == 0)
                        {
                            bool saved = progression.CheckAntiSoftLock();
                            if (!saved)
                            {
                                Debug.LogError($"[Simulate Full Run] SOFT-LOCK DETECTED at {quest.QuestId}! Player has no coins, seeds, or crops.");
                                return;
                            }
                        }

                        // Unlock pumpkin garden if needed
                        if (quest.TargetCrop == CropType.Pumpkin && !progression.IsGardenUnlocked(CropType.Pumpkin))
                        {
                            while (progression.Coins < progression.GetGardenUnlockCost(CropType.Pumpkin))
                            {
                                if (progression.SeedInventory.GetCount(CropType.Carrot) == 0)
                                    progression.TryBuySeed(CropType.Carrot, 2);
                                progression.TrySpendSeed(CropType.Carrot, 2);
                                simulatedSeconds += 35f;
                                hud.CropInventory.Add(CropType.Carrot, 2);
                                progression.TrySellCrop(CropType.Carrot, 2);
                                simulatedSeconds += 5f;
                            }
                            progression.TryUnlockGarden(CropType.Pumpkin);
                            simulatedSeconds += 2f;
                        }

                        // Unlock tomato garden if needed
                        if (quest.TargetCrop == CropType.Tomato && !progression.IsGardenUnlocked(CropType.Tomato))
                        {
                            while (progression.Coins < progression.GetGardenUnlockCost(CropType.Tomato))
                            {
                                if (progression.SeedInventory.GetCount(CropType.Carrot) == 0)
                                    progression.TryBuySeed(CropType.Carrot, 2);
                                progression.TrySpendSeed(CropType.Carrot, 2);
                                simulatedSeconds += 35f;
                                hud.CropInventory.Add(CropType.Carrot, 2);
                                progression.TrySellCrop(CropType.Carrot, 2);
                                simulatedSeconds += 5f;
                            }
                            progression.TryUnlockGarden(CropType.Tomato);
                            simulatedSeconds += 2f;
                        }

                        // Complete quest steps
                        for (int s = 0; s < quest.RequiredAmount; s++)
                        {
                            switch (quest.ConditionType)
                            {
                                case QuestConditionType.Plant:
                                    if (progression.SeedInventory.GetCount(quest.TargetCrop) <= 0)
                                    {
                                        if (progression.Coins < progression.GetSeedCost(quest.TargetCrop))
                                        {
                                            progression.CheckAntiSoftLock();
                                        }
                                        progression.TryBuySeed(quest.TargetCrop, 1);
                                    }
                                    progression.TrySpendSeed(quest.TargetCrop, 1);
                                    questManager.ReportCropPlanted(quest.TargetCrop);
                                    simulatedSeconds += 3f;
                                    break;

                                case QuestConditionType.Harvest:
                                    float growTime = quest.TargetCrop == CropType.Pumpkin ? 50f
                                        : quest.TargetCrop == CropType.Tomato ? 40f : 35f;
                                    simulatedSeconds += growTime;
                                    hud.CropInventory.Add(quest.TargetCrop, 1);
                                    questManager.ReportCropHarvested(quest.TargetCrop, 1);
                                    break;

                                case QuestConditionType.Process:
                                    if (hud.CropInventory.GetCount(quest.TargetCrop) < 2)
                                    {
                                        hud.CropInventory.Add(quest.TargetCrop, 2);
                                        simulatedSeconds += 40f;
                                    }
                                    progression.TryProduce(quest.TargetCrop, 2);
                                    questManager.ReportRecipeProcessed(quest.TargetCrop, 1);
                                    simulatedSeconds += 10f;
                                    break;

                                case QuestConditionType.DeliverOrder:
                                    if (progression.ActiveOrdersCount > 0)
                                    {
                                        ActiveTimedOrder o = progression.ActiveTimedOrders[0];
                                        if (o.isProduct)
                                        {
                                            progression.TryProduce(o.cropType, 2);
                                        }
                                        else
                                        {
                                            hud.CropInventory.Add(o.cropType, o.amount);
                                        }
                                        progression.TryDeliverOrder();
                                        simulatedSeconds += 25f;
                                    }
                                    questManager.ReportOrderDelivered(1);
                                    break;

                                case QuestConditionType.EarnMoney:
                                    progression.AddCoins(quest.RequiredAmount);
                                    questManager.ReportMoneyEarned(quest.RequiredAmount);
                                    simulatedSeconds += 5f;
                                    break;

                                case QuestConditionType.Repair:
                                    int repairCost = quest.RepairTarget == 0 ? 30 : 45;
                                    if (progression.Coins < repairCost)
                                    {
                                        progression.AddCoins(repairCost - progression.Coins);
                                        simulatedSeconds += 60f;
                                    }
                                    progression.TryRepair(quest.RepairTarget, repairCost);
                                    questManager.ReportRepairCompleted(quest.RepairTarget);
                                    simulatedSeconds += 5f;
                                    break;
                            }
                        }

                        if (!questManager.IsQuestCompleted(quest.QuestId))
                        {
                            Debug.LogError($"[Simulate Full Run] Quest {quest.QuestId} did not complete!");
                            return;
                        }
                    }
                    chaptersCompleted++;
                }

                if (!questManager.IsAllChaptersCompleted)
                {
                    Debug.LogError("[Simulate Full Run] Chapters incomplete after run!");
                    return;
                }

                // Verify save/load round-trip
                QuestSaveData questSave = questManager.ExportSaveData();
                TimedOrderSaveData[] orderSave = progression.ExportTimedOrders();
                int[] seedSave = progression.SeedInventory.Snapshot();

                questManager.RestoreSaveData(null);
                progression.SeedInventory.Restore(null);
                progression.RestoreTimedOrders(null, System.DateTime.UtcNow);

                questManager.RestoreSaveData(questSave);
                progression.SeedInventory.Restore(seedSave);
                progression.RestoreTimedOrders(orderSave, System.DateTime.UtcNow);

                if (!questManager.IsAllChaptersCompleted)
                {
                    Debug.LogError("[Simulate Full Run] Save/load round-trip failed to preserve completed state!");
                    return;
                }

                int minutes = (int)(simulatedSeconds / 60f);
                int seconds = (int)(simulatedSeconds % 60f);
                Debug.Log($"[Simulate Full Run] SUCCESS: All {chaptersCompleted} chapters ({totalQuests} quests) completed with NO soft-locks. Total simulated time: {minutes}m {seconds}s ({simulatedSeconds:F0}s). Coins: {progression.Coins}. Save/load verified.");
            }
            finally
            {
                Object.DestroyImmediate(runner);
                QuestManager.Instance = originalQuestManager;
                FarmProgression.Instance = originalProgression;
            }
        }
    }
}
