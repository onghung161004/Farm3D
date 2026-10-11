using UnityEngine;

namespace FarmRestoration
{
    /// <summary>Deterministic shallow basin for the meadow pond; world coordinates in metres.</summary>
    public static class FishingPondTerrain
    {
        public static readonly Vector2 Center = new Vector2(17f, 27f);
        public const float OuterRadiusX = 6f;
        public const float OuterRadiusZ = 5f;
        public const float WaterRadiusX = 4.3f;
        public const float WaterRadiusZ = 3.55f;
        public const float WaterLevel = -0.05f;
        public const float BedLevel = -0.95f;

        public static float Height(float x, float z, float originalHeight)
        {
            float dx = (x - Center.x) / OuterRadiusX;
            float dz = (z - Center.y) / OuterRadiusZ;
            float radius = Mathf.Sqrt(dx * dx + dz * dz);
            if (radius >= 1f) return originalHeight;
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.69f, 1f, radius));
            return Mathf.Lerp(Mathf.Min(originalHeight, BedLevel), originalHeight, blend);
        }

        public static bool IsNear(Vector2 point, float extraClearance = 0f)
        {
            float dx = (point.x - Center.x) / (OuterRadiusX + extraClearance);
            float dz = (point.y - Center.y) / (OuterRadiusZ + extraClearance);
            return dx * dx + dz * dz < 1f;
        }
    }
}
