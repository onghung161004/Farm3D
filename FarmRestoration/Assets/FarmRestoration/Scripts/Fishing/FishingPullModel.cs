using UnityEngine;

namespace FarmRestoration
{
    public struct FishingPullState
    {
        public float tension;
        public float progress;
        public float overpullTime;
        public float slackTime;
        public bool caught;
        public bool lost;

        public static FishingPullState Initial => new FishingPullState { tension = 0.5f };
    }

    /// <summary>Frame-rate-independent reel rules; no scene or input dependencies.</summary>
    public static class FishingPullModel
    {
        public const float SafeHalfWidth = 0.15f;

        public static FishingPullState Step(FishingPullState state, bool held, float deltaTime, float safeCenter)
        {
            if (state.caught || state.lost) return state;
            float center = Mathf.Clamp(safeCenter, SafeHalfWidth, 1f - SafeHalfWidth);
            float remaining = Mathf.Max(0f, deltaTime);
            while (remaining > 0f && !state.caught && !state.lost)
            {
                float dt = Mathf.Min(remaining, 0.05f);
                remaining -= dt;
                state.tension = Mathf.Clamp01(state.tension + (held ? 0.52f : -0.37f) * dt);
                bool inSafeZone = Mathf.Abs(state.tension - center) <= SafeHalfWidth;
                state.progress = Mathf.Clamp01(state.progress + (inSafeZone ? 0.28f : -0.06f) * dt);
                state.overpullTime = state.tension >= 0.92f ? state.overpullTime + dt : 0f;
                state.slackTime = state.tension <= 0.08f ? state.slackTime + dt : 0f;
                state.caught = state.progress >= 1f;
                state.lost = !state.caught && (state.overpullTime >= 1.1f || state.slackTime >= 2.2f);
            }
            return state;
        }
    }
}
