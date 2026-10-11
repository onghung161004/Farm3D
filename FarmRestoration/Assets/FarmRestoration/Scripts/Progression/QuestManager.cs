using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; set; }

        [SerializeField] private QuestDatabase database;
        [SerializeField] private List<QuestChapterData> chapters = new List<QuestChapterData>();

        private int currentChapterIndex;
        private readonly Dictionary<string, int> progressMap = new Dictionary<string, int>();
        private readonly HashSet<string> completedQuests = new HashSet<string>();
        private readonly HashSet<string> unlockedRewards = new HashSet<string>();
        private bool isSubscribed;

        public event Action Changed;
        public event Action<QuestData> QuestCompleted;
        public event Action<QuestChapterData> ChapterCompleted;

        public IReadOnlyList<QuestChapterData> Chapters => database != null && database.Chapters.Count > 0
            ? database.Chapters
            : chapters;

        public int CurrentChapterIndex => currentChapterIndex;

        public QuestChapterData CurrentChapter
        {
            get
            {
                var ch = Chapters;
                return currentChapterIndex >= 0 && currentChapterIndex < ch.Count ? ch[currentChapterIndex] : null;
            }
        }

        public bool IsAllChaptersCompleted => Chapters.Count > 0 && currentChapterIndex >= Chapters.Count;

        private void Awake()
        {
            if (Application.isPlaying && Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (database == null && (chapters == null || chapters.Count == 0))
            {
                database = Resources.Load<QuestDatabase>("Quests/QuestDatabase");
            }
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            CheckPreExistingConditions();
            Changed?.Invoke();
        }

        private void SubscribeEvents()
        {
            if (isSubscribed) return;
            FarmPlot.AnyCropPlanted += OnCropPlanted;
            FarmPlot.AnyCropHarvested += OnCropHarvested;
            FarmProgression.AnyRecipeProcessed += OnRecipeProcessed;
            FarmProgression.AnyOrderDelivered += OnOrderDelivered;
            FarmProgression.AnyMoneyEarned += OnMoneyEarned;
            FarmProgression.AnyRepairCompleted += OnRepairCompleted;
            isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!isSubscribed) return;
            FarmPlot.AnyCropPlanted -= OnCropPlanted;
            FarmPlot.AnyCropHarvested -= OnCropHarvested;
            FarmProgression.AnyRecipeProcessed -= OnRecipeProcessed;
            FarmProgression.AnyOrderDelivered -= OnOrderDelivered;
            FarmProgression.AnyMoneyEarned -= OnMoneyEarned;
            FarmProgression.AnyRepairCompleted -= OnRepairCompleted;
            isSubscribed = false;
        }

        public void Configure(QuestDatabase db)
        {
            database = db;
            chapters.Clear();
            Changed?.Invoke();
        }

        public void Configure(IEnumerable<QuestChapterData> chapterList)
        {
            database = null;
            chapters = new List<QuestChapterData>(chapterList);
            Changed?.Invoke();
        }

        public int GetQuestProgress(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return 0;
            return progressMap.TryGetValue(questId, out int progress) ? progress : 0;
        }

        public bool IsQuestCompleted(string questId)
        {
            return !string.IsNullOrEmpty(questId) && completedQuests.Contains(questId);
        }

        public bool IsRewardUnlocked(string rewardName)
        {
            return !string.IsNullOrEmpty(rewardName) && unlockedRewards.Contains(rewardName);
        }

        private void OnCropPlanted(CropType crop) => ReportCropPlanted(crop);
        private void OnCropHarvested(CropType crop, int amount) => ReportCropHarvested(crop, amount);
        private void OnRecipeProcessed(CropType crop, int amount) => ReportRecipeProcessed(crop, amount);
        private void OnOrderDelivered(int orderIndex) => ReportOrderDelivered(1);
        private void OnMoneyEarned(int amount) => ReportMoneyEarned(amount);
        private void OnRepairCompleted(int index) => ReportRepairCompleted(index);

        public void ReportCropPlanted(CropType crop)
        {
            AdvanceMatchingQuests(q => q.ConditionType == QuestConditionType.Plant && (q.AnyCrop || q.TargetCrop == crop), 1);
        }

        public void ReportCropHarvested(CropType crop, int amount)
        {
            if (amount <= 0) return;
            AdvanceMatchingQuests(q => q.ConditionType == QuestConditionType.Harvest && (q.AnyCrop || q.TargetCrop == crop), amount);
        }

        public void ReportRecipeProcessed(CropType crop, int amount)
        {
            if (amount <= 0) return;
            AdvanceMatchingQuests(q => q.ConditionType == QuestConditionType.Process && (q.AnyCrop || q.TargetCrop == crop), amount);
        }

        public void ReportOrderDelivered(int count = 1)
        {
            if (count <= 0) return;
            AdvanceMatchingQuests(q => q.ConditionType == QuestConditionType.DeliverOrder, count);
        }

        public void ReportMoneyEarned(int amount)
        {
            if (amount <= 0) return;
            AdvanceMatchingQuests(q => q.ConditionType == QuestConditionType.EarnMoney, amount);
        }

        public void ReportRepairCompleted(int targetIndex)
        {
            AdvanceMatchingQuests(q => q.ConditionType == QuestConditionType.Repair && q.RepairTarget == targetIndex, 1);
        }

        private void AdvanceMatchingQuests(Func<QuestData, bool> predicate, int amount)
        {
            QuestChapterData current = CurrentChapter;
            if (current == null || current.Quests == null) return;

            bool anyChanged = false;
            foreach (QuestData quest in current.Quests)
            {
                if (quest == null || IsQuestCompleted(quest.QuestId)) continue;
                if (!predicate(quest)) continue;

                int currentProgress = GetQuestProgress(quest.QuestId);
                int newProgress = currentProgress + amount;
                progressMap[quest.QuestId] = newProgress;
                anyChanged = true;

                if (newProgress >= quest.RequiredAmount)
                {
                    CompleteQuest(quest);
                }
            }

            if (anyChanged)
            {
                CheckChapterCompletion();
                Changed?.Invoke();
            }
        }

        private void CompleteQuest(QuestData quest)
        {
            if (quest == null || completedQuests.Contains(quest.QuestId)) return;
            completedQuests.Add(quest.QuestId);
            progressMap[quest.QuestId] = quest.RequiredAmount;

            if (quest.RewardCoins > 0 && FarmProgression.Instance != null)
            {
                FarmProgression.Instance.AddCoins(quest.RewardCoins);
            }

            if (!string.IsNullOrEmpty(quest.RewardUnlock))
            {
                unlockedRewards.Add(quest.RewardUnlock);
                if (FarmProgression.Instance != null)
                {
                    if (quest.RewardUnlock.IndexOf("Pumpkin", StringComparison.OrdinalIgnoreCase) >= 0)
                        FarmProgression.Instance.UnlockGardenFree(CropType.Pumpkin);
                    else if (quest.RewardUnlock.IndexOf("Tomato", StringComparison.OrdinalIgnoreCase) >= 0)
                        FarmProgression.Instance.UnlockGardenFree(CropType.Tomato);
                    else if (quest.RewardUnlock.IndexOf("Carrot", StringComparison.OrdinalIgnoreCase) >= 0)
                        FarmProgression.Instance.UnlockGardenFree(CropType.Carrot);
                }
            }

            QuestCompleted?.Invoke(quest);
        }

        private void CheckChapterCompletion()
        {
            QuestChapterData current = CurrentChapter;
            if (current == null || current.Quests == null || current.Quests.Count == 0) return;

            foreach (QuestData quest in current.Quests)
            {
                if (quest != null && !IsQuestCompleted(quest.QuestId)) return;
            }

            ChapterCompleted?.Invoke(current);
            currentChapterIndex++;
            CheckPreExistingConditions();
        }

        public void CheckPreExistingConditions()
        {
            QuestChapterData current = CurrentChapter;
            if (current == null || current.Quests == null) return;

            bool anyChanged = false;
            foreach (QuestData quest in current.Quests)
            {
                if (quest == null || IsQuestCompleted(quest.QuestId)) continue;

                if (quest.ConditionType == QuestConditionType.Repair)
                {
                    if (FarmProgression.Instance != null && FarmProgression.Instance.IsRepaired(quest.RepairTarget))
                    {
                        CompleteQuest(quest);
                        anyChanged = true;
                    }
                }
            }

            if (anyChanged)
            {
                CheckChapterCompletion();
                Changed?.Invoke();
            }
        }

        public QuestSaveData ExportSaveData()
        {
            List<QuestProgressSaveEntry> entries = new List<QuestProgressSaveEntry>();
            foreach (var kvp in progressMap)
            {
                entries.Add(new QuestProgressSaveEntry
                {
                    questId = kvp.Key,
                    currentProgress = kvp.Value,
                    isCompleted = completedQuests.Contains(kvp.Key)
                });
            }
            foreach (string completedId in completedQuests)
            {
                if (!progressMap.ContainsKey(completedId))
                {
                    entries.Add(new QuestProgressSaveEntry
                    {
                        questId = completedId,
                        currentProgress = 1,
                        isCompleted = true
                    });
                }
            }

            string[] rewards = new string[unlockedRewards.Count];
            unlockedRewards.CopyTo(rewards);

            return new QuestSaveData
            {
                currentChapterIndex = currentChapterIndex,
                questProgress = entries.ToArray(),
                unlockedRewards = rewards
            };
        }

        public void RestoreSaveData(QuestSaveData save)
        {
            progressMap.Clear();
            completedQuests.Clear();
            unlockedRewards.Clear();

            if (save == null)
            {
                currentChapterIndex = 0;
                CheckPreExistingConditions();
                Changed?.Invoke();
                return;
            }

            currentChapterIndex = Mathf.Clamp(save.currentChapterIndex, 0, Mathf.Max(0, Chapters.Count));

            if (save.questProgress != null)
            {
                foreach (QuestProgressSaveEntry entry in save.questProgress)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.questId)) continue;
                    progressMap[entry.questId] = entry.currentProgress;
                    if (entry.isCompleted) completedQuests.Add(entry.questId);
                }
            }

            if (save.unlockedRewards != null)
            {
                foreach (string reward in save.unlockedRewards)
                {
                    if (!string.IsNullOrEmpty(reward)) unlockedRewards.Add(reward);
                }
            }

            CheckPreExistingConditions();
            Changed?.Invoke();
        }

        public string GetTrackerSummaryText()
        {
            if (IsAllChaptersCompleted)
            {
                return "ALL CHAPTERS COMPLETED\nThe farm has been restored!";
            }

            QuestChapterData current = CurrentChapter;
            if (current == null) return "No active quests";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[{current.ChapterTitle}]");

            if (current.Quests != null)
            {
                foreach (QuestData quest in current.Quests)
                {
                    if (quest == null) continue;
                    bool completed = IsQuestCompleted(quest.QuestId);
                    int progress = GetQuestProgress(quest.QuestId);
                    sb.AppendLine(quest.GetProgressText(progress, completed));
                }
            }

            return sb.ToString().TrimEnd();
        }
    }
}
