using System;

namespace FarmRestoration
{
    [Serializable]
    public sealed class FarmSaveData
    {
        public int version = 1;
        public PlotSaveData[] plots = Array.Empty<PlotSaveData>();
        public int[] crops = new int[3];
        public int[] products = new int[3];
        public int coins;
        public int orderIndex;
        public bool[] repairs = new bool[2];
        public int milk;
        public CowSaveData[] cows = Array.Empty<CowSaveData>();
    }

    [Serializable]
    public sealed class PlotSaveData
    {
        public string id;
        public int state;
        public long readyUtcTicks;
    }

    [Serializable]
    public sealed class CowSaveData
    {
        public string id;
        public bool penned;
        public float x;
        public float z;
        public long milkReadyUtcTicks;
    }
}
