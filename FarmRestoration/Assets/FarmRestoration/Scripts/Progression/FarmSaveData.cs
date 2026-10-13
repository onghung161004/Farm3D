using System;

namespace FarmRestoration
{
    [Serializable]
    public sealed class FarmSaveData
    {
        public const int CurrentVersion = 8;
        public int version = CurrentVersion;
        public PlotSaveData[] plots = Array.Empty<PlotSaveData>();
        public int[] crops = new int[3];
        public int[] products = new int[3];
        public int coins;
        public int orderIndex;
        public bool[] repairs = new bool[2];
        public bool[] gardenUnlocks = new bool[3];
        public int milk;
        public int fish;
        public CowSaveData[] cows = Array.Empty<CowSaveData>();
        public int wood;
        public bool cowPenUpgraded;
        public int bridgeUpgradeLevel;
        public TreeSaveData[] trees = Array.Empty<TreeSaveData>();
        public FarmQuestProgress quests;
    }

    [Serializable]
    public sealed class PlotSaveData
    {
        public string id;
        public int state;
        public long readyUtcTicks;
        public int cropType;
    }

    [Serializable]
    public sealed class CowSaveData
    {
        public string id;
        public bool penned;
        public float x;
        public float z;
        public long milkReadyUtcTicks;
        public bool offspring;
        public string penId;
        public long bornUtcTicks;
        public long grownUtcTicks;
        public long breedingReadyUtcTicks;
        public long fedUntilUtcTicks;
    }

    [Serializable]
    public sealed class TreeSaveData
    {
        public string id;
        public int hits;
        public long regrowUtcTicks;
    }
}
