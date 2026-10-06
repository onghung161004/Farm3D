using System;

namespace FarmRestoration
{
    public sealed class CropInventory
    {
        private readonly int[] counts = new int[3];

        public int Total { get; private set; }
        public event Action<CropType, int> Changed;

        public void Add(CropType cropType, int amount)
        {
            if (amount <= 0) return;
            int index = (int)cropType;
            counts[index] += amount;
            Total += amount;
            Changed?.Invoke(cropType, counts[index]);
        }

        public int GetCount(CropType cropType) => counts[(int)cropType];

        public bool TrySpend(CropType cropType, int amount)
        {
            int index = (int)cropType;
            if (amount <= 0 || counts[index] < amount) return false;
            counts[index] -= amount;
            Total -= amount;
            Changed?.Invoke(cropType, counts[index]);
            return true;
        }

        public int[] Snapshot() => (int[])counts.Clone();

        public void Restore(int[] savedCounts)
        {
            Total = 0;
            for (int i = 0; i < counts.Length; i++)
            {
                counts[i] = savedCounts != null && i < savedCounts.Length ? Math.Max(0, savedCounts[i]) : 0;
                Total += counts[i];
                Changed?.Invoke((CropType)i, counts[i]);
            }
        }

        public string Summary()
        {
            return "Pumpkin: " + GetCount(CropType.Pumpkin)
                + "  Carrot: " + GetCount(CropType.Carrot)
                + "  Tomato: " + GetCount(CropType.Tomato);
        }
    }
}
