using System;
using UnityEngine;

namespace FarmRestoration
{
    /// <summary>Finds one complete 3x3 garden without depending on camera aim.</summary>
    public static class FarmGardenBatch
    {
        private static readonly string[] GardenNames = { "PumpkinGarden", "CarrotGarden", "TomatoGarden" };

        public static Transform FindNearest(Vector3 playerPosition, float range)
        {
            FarmPlot[] plots = UnityEngine.Object.FindObjectsByType<FarmPlot>();
            Transform nearest = null;
            float bestDistance = range * range;

            for (int i = 0; i < plots.Length; i++)
            {
                FarmPlot plot = plots[i];
                if (plot == null || !plot.isActiveAndEnabled) continue;
                Transform garden = FindGarden(plot.transform);
                if (garden == null || garden.GetComponentsInChildren<FarmPlot>().Length != 9) continue;

                float distance = HorizontalDistanceSquared(playerPosition, plot.transform.position);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                nearest = garden;
            }

            return nearest;
        }

        public static bool IsWithinRange(Transform garden, Vector3 playerPosition, float range)
        {
            if (garden == null || !garden.gameObject.activeInHierarchy) return false;
            float maxDistance = range * range;
            foreach (FarmPlot plot in garden.GetComponentsInChildren<FarmPlot>())
            {
                if (plot != null && plot.isActiveAndEnabled
                    && HorizontalDistanceSquared(playerPosition, plot.transform.position) <= maxDistance)
                    return true;
            }
            return false;
        }

        public static bool IsGardenPlot(FarmPlot plot)
        {
            Transform garden = plot == null ? null : FindGarden(plot.transform);
            return garden != null && garden.GetComponentsInChildren<FarmPlot>().Length == 9;
        }

        public static FarmPlot[] GetOrderedPlots(Transform garden, Vector3 playerPosition)
        {
            if (garden == null || !garden.gameObject.activeInHierarchy) return Array.Empty<FarmPlot>();
            FarmPlot[] plots = garden.GetComponentsInChildren<FarmPlot>();
            if (plots.Length != 9) return Array.Empty<FarmPlot>();
            Array.Sort(plots, (a, b) =>
            {
                int distanceOrder = HorizontalDistanceSquared(playerPosition, a.transform.position)
                    .CompareTo(HorizontalDistanceSquared(playerPosition, b.transform.position));
                return distanceOrder != 0 ? distanceOrder : string.CompareOrdinal(a.name, b.name);
            });
            return plots;
        }

        private static Transform FindGarden(Transform child)
        {
            for (Transform current = child.parent; current != null; current = current.parent)
            {
                for (int i = 0; i < GardenNames.Length; i++)
                    if (current.name == GardenNames[i]) return current;
            }
            return null;
        }

        private static float HorizontalDistanceSquared(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z;
        }
    }
}
