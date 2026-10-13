using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmRestoration
{
    /// <summary>Pairs fed adult cows and recreates dynamically born cows when loading a save.</summary>
    public static class CowBreeding
    {
        private static readonly Collider[] birthObstacles = new Collider[16];

        public static CowAnimal TryBreed(CowAnimal fedCow, IReadOnlyList<CowAnimal> herd,
            DateTime now, out string message)
        {
            message = null;
            if (fedCow == null || herd == null || !fedCow.IsPenned || fedCow.HomePen == null) return null;
            if (!fedCow.LifeCycle.CanBreed(now))
            {
                message = "Cow fed for milk. Breeding will be ready after its cooldown.";
                return null;
            }

            CowPen pen = fedCow.HomePen;
            CowAnimal partner = FindPartner(fedCow, herd, now, out int pennedCount);
            if (pennedCount >= pen.Capacity)
            {
                message = "Cow fed for milk. Pen full (" + pen.Capacity + " cows): no room for a calf.";
                return null;
            }
            if (partner == null)
            {
                message = "Cow fed! Feed another adult in this pen within 60s to have a calf.";
                return null;
            }
            if (!TryFindBirthPosition(pen, fedCow, herd, out Vector3 position))
            {
                message = "Cow fed for milk. No clear space in the pen for a calf.";
                return null;
            }
            var saved = new CowSaveData
            {
                id = "calf-" + Guid.NewGuid().ToString("N"),
                penned = true,
                offspring = true,
                penId = pen.PenId,
                x = position.x,
                z = position.z,
                bornUtcTicks = now.Ticks,
                grownUtcTicks = now.AddSeconds(CowLifeCycle.GrowthSeconds).Ticks
            };
            CowAnimal calf = CowOffspringFactory.Create(fedCow, saved);
            if (calf == null) return null;
            fedCow.LifeCycle.CompleteBreeding(now);
            partner.LifeCycle.CompleteBreeding(now);
            fedCow.NotifyLifeCycleChanged();
            partner.NotifyLifeCycleChanged();
            message = "A calf was born! It grows into an adult in 5 minutes.";
            return calf;
        }

        private static CowAnimal FindPartner(CowAnimal fedCow, IReadOnlyList<CowAnimal> herd,
            DateTime now, out int pennedCount)
        {
            CowAnimal partner = null;
            pennedCount = 0;
            foreach (CowAnimal cow in herd)
            {
                if (cow == null || !cow.IsPenned || cow.HomePen != fedCow.HomePen) continue;
                pennedCount++;
                if (cow != fedCow && cow.LifeCycle.CanBreed(now)) partner = cow;
            }
            return partner;
        }

        public static void RestoreOffspring(CowSaveData[] savedCows, List<CowAnimal> herd)
        {
            if (savedCows == null || herd == null) return;
            foreach (CowSaveData saved in savedCows)
            {
                if (saved == null || !saved.offspring || string.IsNullOrEmpty(saved.id)) continue;
                if (herd.Exists(cow => cow != null && cow.CowId == saved.id)) continue;
                CowAnimal template = herd.Find(cow => cow != null && cow.HomePen != null
                    && !cow.LifeCycle.IsOffspring && cow.HomePen.PenId == saved.penId);
                if (template == null)
                {
                    Debug.LogWarning("Could not restore offspring " + saved.id + ": its pen/template is missing.");
                    continue;
                }
                CowAnimal calf = CowOffspringFactory.Create(template, saved);
                if (calf != null) herd.Add(calf);
            }
        }

        private static bool TryFindBirthPosition(CowPen pen, CowAnimal parent,
            IReadOnlyList<CowAnimal> herd, out Vector3 position)
        {
            position = default;
            float nearest = float.MaxValue;
            Vector2 size = pen.Footprint;
            for (float x = -size.x * 0.5f + 1.3f; x <= size.x * 0.5f - 1.3f; x += 2.4f)
            for (float z = -size.y * 0.5f + 1.3f; z <= size.y * 0.5f - 1.3f; z += 2.4f)
            {
                Vector3 candidate = pen.transform.TransformPoint(new Vector3(x, 0f, z));
                bool occupied = false;
                foreach (CowAnimal cow in herd)
                {
                    if (cow == null || cow.HomePen != pen) continue;
                    Vector3 separation = candidate - cow.transform.position;
                    separation.y = 0f;
                    if (separation.sqrMagnitude < 2f * 2f) { occupied = true; break; }
                }
                if (occupied) continue;
                if (parent.Ground != null)
                {
                    if (!parent.Ground.Raycast(new Ray(candidate + Vector3.up * 20f, Vector3.down),
                        out RaycastHit hit, 40f)) continue;
                    candidate.y = hit.point.y;
                }
                if (!IsBirthSpaceClear(candidate, parent.Ground)) continue;
                float distance = (candidate - parent.transform.position).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance;
                position = candidate;
            }
            return nearest < float.MaxValue;
        }

        private static bool IsBirthSpaceClear(Vector3 position, Collider ground)
        {
            int count = Physics.OverlapSphereNonAlloc(position + Vector3.up * 0.8f, 0.6f,
                birthObstacles, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == birthObstacles.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Collider obstacle = birthObstacles[i];
                if (obstacle != ground && obstacle.GetComponentInParent<CowAnimal>() == null) return false;
            }
            return true;
        }
    }
}
