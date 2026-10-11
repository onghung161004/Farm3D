using System;
using UnityEngine;

namespace FarmRestoration
{
    [CreateAssetMenu(fileName = "NewQuest", menuName = "Farm Restoration/Quest Data")]
    public sealed class QuestData : ScriptableObject
    {
        [SerializeField] private string questId = "quest_01";
        [SerializeField] private string title = "New Quest";
        [SerializeField, TextArea(2, 4)] private string description = "";
        [SerializeField] private QuestConditionType conditionType = QuestConditionType.Plant;
        [SerializeField] private CropType targetCrop = CropType.Pumpkin;
        [SerializeField] private bool anyCrop;
        [SerializeField, Min(1)] private int requiredAmount = 1;
        [SerializeField] private int repairTarget;
        [SerializeField, Min(0)] private int rewardCoins;
        [SerializeField] private string rewardUnlock = "";

        public string QuestId => questId;
        public string Title => title;
        public string Description => description;
        public QuestConditionType ConditionType => conditionType;
        public CropType TargetCrop => targetCrop;
        public bool AnyCrop => anyCrop;
        public int RequiredAmount => Mathf.Max(1, requiredAmount);
        public int RepairTarget => repairTarget;
        public int RewardCoins => rewardCoins;
        public string RewardUnlock => rewardUnlock;

        public void Configure(
            string id,
            string questTitle,
            string desc,
            QuestConditionType type,
            CropType crop,
            bool any,
            int amount,
            int repair,
            int coins,
            string unlock = "")
        {
            questId = id;
            title = questTitle;
            description = desc;
            conditionType = type;
            targetCrop = crop;
            anyCrop = any;
            requiredAmount = Mathf.Max(1, amount);
            repairTarget = repair;
            rewardCoins = Mathf.Max(0, coins);
            rewardUnlock = unlock ?? "";
        }

        public string GetProgressText(int currentProgress, bool isCompleted)
        {
            if (isCompleted) return $"[DONE] {title}";
            int current = Mathf.Clamp(currentProgress, 0, RequiredAmount);
            return $"{title}: {GetConditionDescription()} ({current}/{RequiredAmount})";
        }

        public string GetConditionDescription()
        {
            switch (conditionType)
            {
                case QuestConditionType.Plant:
                    return anyCrop ? $"Plant {RequiredAmount} crops" : $"Plant {RequiredAmount} {targetCrop}";
                case QuestConditionType.Harvest:
                    return anyCrop ? $"Harvest {RequiredAmount} crops" : $"Harvest {RequiredAmount} {targetCrop}";
                case QuestConditionType.Process:
                    return anyCrop ? $"Process {RequiredAmount} recipes" : $"Process {RequiredAmount} {FarmProgression.ProductName(targetCrop)}";
                case QuestConditionType.DeliverOrder:
                    return RequiredAmount == 1 ? "Deliver 1 order" : $"Deliver {RequiredAmount} orders";
                case QuestConditionType.EarnMoney:
                    return $"Earn {RequiredAmount} coins";
                case QuestConditionType.Repair:
                    string repairName = repairTarget == 0 ? "village house" : "bridge approach";
                    return $"Repair {repairName}";
                default:
                    return description;
            }
        }
    }
}
