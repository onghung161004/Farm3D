using UnityEngine;

namespace FarmRestoration
{
    public static class PlayerToolVisualState
    {
        public static string GetVisualRootName(FarmTool tool)
        {
            switch (tool)
            {
                case FarmTool.Hoe: return "HoeTool";
                case FarmTool.Seeds: return "SeedsTool";
                case FarmTool.WateringCan: return "WateringCanTool";
                case FarmTool.Harvest: return "HarvestTool";
                default: return "HoeTool";
            }
        }

        public static Vector3 GetUseBobOffset(float normalizedProgress, float distance)
        {
            return Vector3.down * (Mathf.Sin(Mathf.Clamp01(normalizedProgress) * Mathf.PI) * distance);
        }
    }
}
