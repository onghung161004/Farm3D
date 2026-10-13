using System;
using UnityEngine;

namespace FarmRestoration.World
{
    [DisallowMultipleComponent]
    public sealed class FarmWeatherController : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float clearDurationSeconds = 90f;
        [SerializeField, Min(1f)] private float rainDurationSeconds = 30f;

        private FarmPlot[] plots = Array.Empty<FarmPlot>();
        private float phaseSeconds;
        private const float TransitionTolerance = 0.000001f;

        public FarmWeatherKind CurrentWeather { get; private set; } = FarmWeatherKind.Clear;
        public event Action<FarmWeatherKind> WeatherChanged;

        private void Start()
        {
            plots = FindObjectsByType<FarmPlot>();
        }

        private void Update()
        {
            Advance(Time.deltaTime, DateTime.UtcNow);
        }

        public void ConfigurePlots(FarmPlot[] farmPlots)
        {
            plots = farmPlots ?? Array.Empty<FarmPlot>();
        }

        public void ConfigureDurations(float clearSeconds, float rainSeconds)
        {
            clearDurationSeconds = Mathf.Max(1f, clearSeconds);
            rainDurationSeconds = Mathf.Max(1f, rainSeconds);
        }

        public void Advance(float seconds, DateTime nowUtc)
        {
            if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;

            // A crop can be planted after rain starts. Recheck throughout the rainy phase;
            // TryWaterFromRain only accepts Seeded, so existing growth deadlines are untouched.
            if (CurrentWeather == FarmWeatherKind.Rain) WaterSeededPlots(nowUtc);

            float clear = Mathf.Max(1f, clearDurationSeconds);
            float rain = Mathf.Max(1f, rainDurationSeconds);
            float remaining = seconds;
            // Complete at most one full cycle explicitly. Whole later cycles have the same
            // final phase; seeded plots can only be watered once.
            if (remaining > clear + rain + clear + rain)
                remaining = clear + rain + Mathf.Repeat(remaining - clear - rain, clear + rain);

            for (int transition = 0; transition < 6 && remaining > 0f; transition++)
            {
                float duration = CurrentWeather == FarmWeatherKind.Clear ? clear : rain;
                float untilNext = duration - phaseSeconds;
                if (remaining + TransitionTolerance < untilNext)
                {
                    phaseSeconds += remaining;
                    return;
                }

                remaining = Mathf.Max(0f, remaining - untilNext);
                phaseSeconds = 0f;
                CurrentWeather = CurrentWeather == FarmWeatherKind.Clear
                    ? FarmWeatherKind.Rain : FarmWeatherKind.Clear;
                if (CurrentWeather == FarmWeatherKind.Rain) WaterSeededPlots(nowUtc);
                WeatherChanged?.Invoke(CurrentWeather);
            }
        }

        private void WaterSeededPlots(DateTime nowUtc)
        {
            foreach (FarmPlot plot in plots)
            {
                if (plot != null && plot.isActiveAndEnabled) plot.TryWaterFromRain(nowUtc);
            }
        }
    }
}
