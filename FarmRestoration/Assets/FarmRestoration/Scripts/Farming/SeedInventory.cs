using System;
using UnityEngine;

namespace FarmRestoration
{
    public sealed class SeedInventory
    {
        private readonly int[] counts = new int[3];

        public int TotalSeeds => counts[0] + counts[1] + counts[2];
        public event Action<CropType, int> Changed;

        public SeedInventory(int initialPumpkin = 2, int initialCarrot = 3, int initialTomato = 1)
        {
            counts[(int)CropType.Pumpkin] = initialPumpkin;
            counts[(int)CropType.Carrot] = initialCarrot;
            counts[(int)CropType.Tomato] = initialTomato;
        }

        public int GetCount(CropType crop)
        {
            int index = (int)crop;
            return index >= 0 && index < counts.Length ? counts[index] : 0;
        }

        public void Add(CropType crop, int amount)
        {
            if (amount <= 0) return;
            int index = (int)crop;
            if (index < 0 || index >= counts.Length) return;
            counts[index] += amount;
            Changed?.Invoke(crop, counts[index]);
        }

        public bool TrySpend(CropType crop, int amount = 1)
        {
            if (amount <= 0) return false;
            int index = (int)crop;
            if (index < 0 || index >= counts.Length || counts[index] < amount) return false;
            counts[index] -= amount;
            Changed?.Invoke(crop, counts[index]);
            return true;
        }

        public int[] Snapshot() => (int[])counts.Clone();

        public void Restore(int[] savedCounts)
        {
            if (savedCounts == null || savedCounts.Length == 0)
            {
                // Default starting seeds: allows first harvest
                counts[(int)CropType.Pumpkin] = 2;
                counts[(int)CropType.Carrot] = 3;
                counts[(int)CropType.Tomato] = 1;
            }
            else
            {
                for (int i = 0; i < counts.Length; i++)
                {
                    counts[i] = i < savedCounts.Length ? Math.Max(0, savedCounts[i]) : 0;
                }
            }
            for (int i = 0; i < counts.Length; i++)
            {
                Changed?.Invoke((CropType)i, counts[i]);
            }
        }

        public string Summary()
        {
            return $"Carrot Seeds: {GetCount(CropType.Carrot)} | Pumpkin Seeds: {GetCount(CropType.Pumpkin)} | Tomato Seeds: {GetCount(CropType.Tomato)}";
        }
    }
}
