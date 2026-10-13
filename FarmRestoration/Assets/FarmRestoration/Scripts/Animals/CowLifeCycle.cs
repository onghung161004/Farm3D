using System;
using UnityEngine;

namespace FarmRestoration
{
    /// <summary>UTC-based calf growth and single-use feeding windows for breeding.</summary>
    [DisallowMultipleComponent]
    public sealed class CowLifeCycle : MonoBehaviour
    {
        public const float FeedingWindowSeconds = 60f;
        public const float BreedingCooldownSeconds = 300f;
        public const float GrowthSeconds = 300f;
        public const float CalfScale = 0.45f;

        private Vector3 adultScale;
        private bool scaleCaptured;
        private bool wasCalf;

        public bool IsOffspring { get; private set; }
        public long BornUtcTicks { get; private set; }
        public long GrownUtcTicks { get; private set; }
        public long BreedingReadyUtcTicks { get; private set; }
        public long FedUntilUtcTicks { get; private set; }
        public Vector3 AdultScale
        {
            get
            {
                if (!scaleCaptured) SetAdultScale(transform.localScale);
                return adultScale;
            }
        }

        private void Awake() => SetAdultScale(transform.localScale);
        private void Update() => Tick(DateTime.UtcNow);

        public void SetAdultScale(Vector3 scale)
        {
            adultScale = scale;
            scaleCaptured = true;
        }

        public bool IsCalfAt(DateTime now) => IsOffspring && GrownUtcTicks > now.Ticks;
        public bool CanBreed(DateTime now) => !IsCalfAt(now)
            && FedUntilUtcTicks > now.Ticks && BreedingReadyUtcTicks <= now.Ticks;

        public void RecordFeed(DateTime now)
        {
            FedUntilUtcTicks = !IsCalfAt(now) && BreedingReadyUtcTicks <= now.Ticks
                ? now.AddSeconds(FeedingWindowSeconds).Ticks : 0;
        }

        public void CompleteBreeding(DateTime now)
        {
            FedUntilUtcTicks = 0;
            BreedingReadyUtcTicks = now.AddSeconds(BreedingCooldownSeconds).Ticks;
        }

        public void Restore(CowSaveData saved, DateTime now)
        {
            if (saved == null) return;
            IsOffspring = saved.offspring;
            BornUtcTicks = Math.Max(0, saved.bornUtcTicks);
            GrownUtcTicks = Math.Max(0, saved.grownUtcTicks);
            BreedingReadyUtcTicks = Math.Max(0, saved.breedingReadyUtcTicks);
            FedUntilUtcTicks = Math.Max(0, saved.fedUntilUtcTicks);
            wasCalf = IsCalfAt(now);
            Tick(now);
        }

        public void WriteTo(CowSaveData saved)
        {
            saved.offspring = IsOffspring;
            saved.bornUtcTicks = BornUtcTicks;
            saved.grownUtcTicks = GrownUtcTicks;
            saved.breedingReadyUtcTicks = BreedingReadyUtcTicks;
            saved.fedUntilUtcTicks = FedUntilUtcTicks;
        }

        public void Tick(DateTime now)
        {
            float growth = GrownUtcTicks > BornUtcTicks
                ? Mathf.Clamp01((float)((now.Ticks - (double)BornUtcTicks) / (GrownUtcTicks - (double)BornUtcTicks))) : 1f;
            transform.localScale = AdultScale * (IsOffspring ? Mathf.Lerp(CalfScale, 1f, growth) : 1f);
            bool calf = IsCalfAt(now);
            if (wasCalf && !calf) GetComponent<CowAnimal>()?.NotifyLifeCycleChanged();
            wasCalf = calf;
        }
    }
}
