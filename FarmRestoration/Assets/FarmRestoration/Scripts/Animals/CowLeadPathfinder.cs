using System.Collections.Generic;
using UnityEngine;

namespace FarmRestoration
{
    /// <summary>Small ground-grid route for the cow guide; it does not move the cow.</summary>
    public static class CowLeadPathfinder
    {
        private const float CellSize = 2f;
        private const float Margin = 11f;
        private static readonly Collider[] Obstacles = new Collider[24];

        public static Vector3[] Find(Vector3 start, CowPen pen, MeshCollider ground,
            MeshFilter river, Transform player, Transform cow, MeshCollider bridge = null)
        {
            if (pen == null || ground == null) return System.Array.Empty<Vector3>();
            Vector3 gate = pen.GatePosition;
            Vector3 approach = gate - pen.transform.forward * 2f;
            Vector3 inside = gate + pen.transform.forward * 2.5f;
            Vector2[] waterline = GetRiverCenterline(river);
            if (!TryGround(ground, bridge, gate, waterline, player, cow, out Vector3 groundedGate))
                groundedGate = gate + Vector3.up * 0.35f;
            if (!TryGround(ground, bridge, approach, waterline, player, cow, out approach)
                || !TryGround(ground, bridge, inside, waterline, player, cow, out inside))
                return System.Array.Empty<Vector3>();
            Vector3[] path = FindTo(start, approach, ground, river, player, cow, bridge);
            if (path.Length < 2) return System.Array.Empty<Vector3>();
            List<Vector3> result = new List<Vector3>(path) { groundedGate, inside };
            return result.ToArray();
        }

        /// <summary>Finds a ground route to any destination, ignoring the player and target colliders.</summary>
        public static Vector3[] FindTo(Vector3 start, Vector3 approach, MeshCollider ground,
            MeshFilter river, Transform player, Transform cow, MeshCollider bridge = null)
        {
            if (ground == null) return System.Array.Empty<Vector3>();
            Vector2[] waterline = GetRiverCenterline(river);
            if (!TryGround(ground, bridge, approach, waterline, player, cow, out approach))
                return System.Array.Empty<Vector3>();
            if (!TryGround(ground, bridge, start, waterline, player, cow, out Vector3 groundedStart))
                groundedStart = start + Vector3.up * 0.35f;

            if (ClearSegment(groundedStart, approach, ground, bridge, waterline, player, cow))
            {
                // Sample the surface even on a clear route so the line follows hills instead of cutting through them.
                int samples = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(groundedStart, approach) / 0.8f));
                Vector3[] direct = new Vector3[samples + 1];
                direct[0] = groundedStart;
                for (int i = 1; i <= samples; i++)
                    TryGround(ground, bridge, Vector3.Lerp(groundedStart, approach, i / (float)samples),
                        waterline, player, cow, out direct[i]);
                return direct;
            }

            float minX = Mathf.Min(start.x, approach.x) - Margin;
            float minZ = Mathf.Min(start.z, approach.z) - Margin;
            int width = Mathf.CeilToInt((Mathf.Max(start.x, approach.x) - minX + Margin) / CellSize) + 1;
            int height = Mathf.CeilToInt((Mathf.Max(start.z, approach.z) - minZ + Margin) / CellSize) + 1;
            if (width > 80 || height > 80) return System.Array.Empty<Vector3>();
            int count = width * height;
            int startX = Mathf.Clamp(Mathf.RoundToInt((start.x - minX) / CellSize), 0, width - 1);
            int startZ = Mathf.Clamp(Mathf.RoundToInt((start.z - minZ) / CellSize), 0, height - 1);
            int goalX = Mathf.Clamp(Mathf.RoundToInt((approach.x - minX) / CellSize), 0, width - 1);
            int goalZ = Mathf.Clamp(Mathf.RoundToInt((approach.z - minZ) / CellSize), 0, height - 1);
            int startIndex = startZ * width + startX;
            int goalIndex = goalZ * width + goalX;
            float[] cost = new float[count];
            int[] previous = new int[count];
            byte[] state = new byte[count]; // 0 unknown, 1 walkable, 2 blocked
            bool[] closed = new bool[count];
            Vector3[] positions = new Vector3[count];
            for (int i = 0; i < count; i++) { cost[i] = float.PositiveInfinity; previous[i] = -1; }
            cost[startIndex] = 0f;
            positions[startIndex] = groundedStart;
            state[startIndex] = 1;
            positions[goalIndex] = approach;
            state[goalIndex] = 1;
            List<int> open = new List<int> { startIndex };

            while (open.Count > 0)
            {
                int bestAt = 0;
                float bestScore = float.PositiveInfinity;
                for (int i = 0; i < open.Count; i++)
                {
                    int index = open[i];
                    int x = index % width, z = index / width;
                    float score = cost[index] + Mathf.Sqrt((x - goalX) * (x - goalX) + (z - goalZ) * (z - goalZ));
                    if (score >= bestScore) continue;
                    bestScore = score;
                    bestAt = i;
                }
                int current = open[bestAt];
                open.RemoveAt(bestAt);
                if (current == goalIndex) break;
                closed[current] = true;
                int cx = current % width, cz = current / width;
                for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0) continue;
                    int nx = cx + dx, nz = cz + dz;
                    if (nx < 0 || nx >= width || nz < 0 || nz >= height) continue;
                    int next = nz * width + nx;
                    if (closed[next] || !Walkable(next)) continue;
                    if (Mathf.Abs(positions[next].y - positions[current].y) > 1.2f) continue;
                    if (dx != 0 && dz != 0
                        && (!Walkable(cz * width + nx) || !Walkable(nz * width + cx))) continue;
                    float newCost = cost[current] + (dx != 0 && dz != 0 ? 1.4142f : 1f);
                    if (newCost >= cost[next]) continue;
                    cost[next] = newCost;
                    previous[next] = current;
                    if (!open.Contains(next)) open.Add(next);
                }
            }

            if (goalIndex != startIndex && previous[goalIndex] < 0) return System.Array.Empty<Vector3>();
            List<Vector3> route = new List<Vector3> { approach };
            for (int index = goalIndex; index != startIndex && index >= 0; index = previous[index])
                route.Add(positions[index]);
            route.Add(groundedStart);
            route.Reverse();
            return route.ToArray();

            bool Walkable(int index)
            {
                if (state[index] != 0) return state[index] == 1;
                int x = index % width, z = index / width;
                Vector3 probe = new Vector3(minX + x * CellSize, 0f, minZ + z * CellSize);
                bool valid = TryGround(ground, bridge, probe, waterline, player, cow, out positions[index]);
                state[index] = valid ? (byte)1 : (byte)2;
                return valid;
            }
        }

        private static bool ClearSegment(Vector3 a, Vector3 b, MeshCollider ground, MeshCollider bridge,
            Vector2[] waterline, Transform player, Transform cow)
        {
            int steps = Mathf.CeilToInt(Vector3.Distance(a, b) / 0.8f);
            float previousHeight = a.y;
            for (int i = 1; i <= steps; i++)
            {
                Vector3 point = Vector3.Lerp(a, b, i / (float)steps);
                if (!TryGround(ground, bridge, point, waterline, player, cow, out Vector3 surface)
                    || Mathf.Abs(surface.y - previousHeight) > 1.2f) return false;
                previousHeight = surface.y;
            }
            return true;
        }

        private static bool TryGround(MeshCollider ground, MeshCollider bridge, Vector3 point, Vector2[] waterline,
            Transform player, Transform cow, out Vector3 surface)
        {
            surface = default;
            Ray ray = new Ray(new Vector3(point.x, 100f, point.z), Vector3.down);
            RaycastHit bridgeHit = default;
            bool onBridge = bridge != null && bridge.enabled && bridge.Raycast(ray, out bridgeHit, 200f);
            if (!onBridge && NearRiver(point, waterline)) return false;
            RaycastHit hit;
            if (onBridge) hit = bridgeHit;
            else if (!ground.Raycast(ray, out hit, 200f)) return false;
            surface = hit.point + Vector3.up * 0.35f;
            int count = Physics.OverlapSphereNonAlloc(hit.point + Vector3.up * 0.85f, 0.55f,
                Obstacles, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider obstacle = Obstacles[i];
                if (obstacle == null || obstacle == ground || obstacle == bridge
                    || (player != null && obstacle.transform.IsChildOf(player))
                    || (cow != null && obstacle.transform.IsChildOf(cow))
                    || obstacle.bounds.max.y <= hit.point.y + 0.35f
                    || IsWalkableFloor(obstacle, ray, hit.point.y)) continue;
                return false;
            }
            return true;
        }

        private static bool IsWalkableFloor(Collider obstacle, Ray downwardRay, float groundHeight)
        {
            // Roads and other thin walkable meshes have tall world bounds on slopes.
            // Check the surface at this cell instead of treating the entire mesh as a wall.
            return obstacle.Raycast(downwardRay, out RaycastHit floorHit, 200f)
                && floorHit.normal.y > 0.5f
                && Mathf.Abs(floorHit.point.y - groundHeight) <= 0.55f;
        }

        private static Vector2[] GetRiverCenterline(MeshFilter river)
        {
            Mesh mesh = river == null ? null : river.sharedMesh;
            if (mesh == null || mesh.vertexCount % 9 != 0) return null;
            Vector3[] vertices = mesh.vertices;
            Vector2[] points = new Vector2[vertices.Length / 9];
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 world = river.transform.TransformPoint(vertices[i * 9 + 4]);
                points[i] = new Vector2(world.x, world.z);
            }
            return points;
        }

        private static bool NearRiver(Vector3 point, Vector2[] centerline)
        {
            if (centerline == null) return false;
            Vector2 location = new Vector2(point.x, point.z);
            for (int i = 0; i < centerline.Length - 1; i++)
            {
                Vector2 segment = centerline[i + 1] - centerline[i];
                float lengthSquared = segment.sqrMagnitude;
                float t = lengthSquared < 0.001f ? 0f
                    : Mathf.Clamp01(Vector2.Dot(location - centerline[i], segment) / lengthSquared);
                if ((location - (centerline[i] + segment * t)).sqrMagnitude < 5.4f * 5.4f) return true;
            }
            return false;
        }
    }
}
