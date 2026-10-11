using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmRestoration
{
    public readonly struct VillageHouseSlot
    {
        public readonly string Name;
        public readonly Vector2 Position;
        public readonly float Scale;
        public readonly float RotationY;

        public VillageHouseSlot(string name, float x, float z, float scale, float rotationY)
        {
            Name = name;
            Position = new Vector2(x, z);
            Scale = scale;
            RotationY = rotationY;
        }
    }

    public static class VillageHouseLayout
    {
        private static readonly VillageHouseSlot[] HouseSlots =
        {
            new VillageHouseSlot("WoodenHouseWest", -86f, 65f, 0.80f, 125f),
            new VillageHouseSlot("WoodenHouseNorth", -66f, 79f, 0.82f, 185f),
            new VillageHouseSlot("WoodenHouseSouth", -86f, 34f, 0.80f, 55f),
            new VillageHouseSlot("WoodenHouseOuterWest", -83f, 50f, 0.73f, 80f),
            new VillageHouseSlot("WoodenHouseNorthWest", -84f, 84f, 0.76f, 215f),
            new VillageHouseSlot("WoodenHouseNorthEast", -47f, 79f, 0.78f, 155f),
            new VillageHouseSlot("WoodenHouseSouthEast", -65f, 29f, 0.78f, 25f)
        };

        public static IReadOnlyList<VillageHouseSlot> Homes { get; } = Array.AsReadOnly(HouseSlots);

        public static bool IsNearHome(Vector2 point, float clearance)
        {
            for (int i = 0; i < HouseSlots.Length; i++)
                if (Vector2.Distance(point, HouseSlots[i].Position) < clearance) return true;
            return false;
        }

        /// <summary>Keeps each wooden house on a level patch, with a soft edge into the existing hillside.</summary>
        public static float LevelHouseGround(float x, float z, Func<float, float, float> naturalHeight)
        {
            float original = naturalHeight(x, z);
            Vector2 point = new Vector2(x, z);
            for (int i = 0; i < HouseSlots.Length; i++)
            {
                VillageHouseSlot house = HouseSlots[i];
                float distance = Vector2.Distance(point, house.Position);
                if (distance >= 7.5f) continue;
                float blend = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5.5f, 7.5f, distance));
                return Mathf.Lerp(original, naturalHeight(house.Position.x, house.Position.y), blend);
            }
            return original;
        }

        public static bool Validate(IReadOnlyList<VillageHouseSlot> slots)
        {
            if (slots == null || slots.Count < 4) return false;
            HashSet<string> names = new HashSet<string>();
            Vector2 well = new Vector2(-63f, 50.5f);
            Vector2 bridge = new Vector2(-41f, 39f);
            Vector2 roadStart = new Vector2(-53f, 45f);
            Vector2 roadEnd = new Vector2(-70f, 51f);
            for (int i = 0; i < slots.Count; i++)
            {
                VillageHouseSlot house = slots[i];
                if (string.IsNullOrEmpty(house.Name) || !names.Add(house.Name)
                    || house.Scale <= 0f || Mathf.Abs(house.Position.x) > 93f
                    || Mathf.Abs(house.Position.y) > 93f
                    || Vector2.Distance(house.Position, well) < 14f
                    || Vector2.Distance(house.Position, bridge) < 18f
                    || DistanceToSegment(house.Position, roadStart, roadEnd) < 8f
                    || DistanceToSegment(house.Position, new Vector2(-97f, 32f), new Vector2(-97f, 89f)) < 7f
                    || DistanceToSegment(house.Position, new Vector2(-38f, 56f), new Vector2(-38f, 86f)) < 7f
                    || DistanceToSegment(house.Position, new Vector2(-97f, 94f), new Vector2(-40f, 94f)) < 7f)
                    return false;
                for (int j = 0; j < i; j++)
                    if (Vector2.Distance(house.Position, slots[j].Position) < 15f) return false;
            }
            return names.Contains("WoodenHouseSouth");
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 line = end - start;
            float t = Mathf.Clamp01(Vector2.Dot(point - start, line) / line.sqrMagnitude);
            return Vector2.Distance(point, start + line * t);
        }
    }
}
