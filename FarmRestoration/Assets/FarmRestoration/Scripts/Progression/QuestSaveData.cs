using System;

namespace FarmRestoration
{
    [Serializable]
    public sealed class QuestSaveData
    {
        public int currentChapterIndex;
        public QuestProgressSaveEntry[] questProgress = Array.Empty<QuestProgressSaveEntry>();
        public string[] unlockedRewards = Array.Empty<string>();
    }

    [Serializable]
    public sealed class QuestProgressSaveEntry
    {
        public string questId;
        public int currentProgress;
        public bool isCompleted;
    }
}
