using FarmRestoration.World;
using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class DaylightTests
    {
        [Test]
        public void ClearRainAndClearAgainAlwaysKeepDaylightAndNpcSchedule()
        {
            GameObject lightObject = new GameObject("DaylightTest", typeof(Light));
            Color ambient = RenderSettings.ambientLight;
            Color fogColor = RenderSettings.fogColor;
            bool fog = RenderSettings.fog;
            float density = RenderSettings.fogDensity;
            Material sky = RenderSettings.skybox;
            try
            {
                RenderSettings.skybox = null;
                Light sun = lightObject.GetComponent<Light>();
                sun.type = LightType.Directional;
                DayNightCycle clock = lightObject.AddComponent<DayNightCycle>();
                clock.SetRainBlend(0f);
                float dayIntensity = sun.intensity;
                clock.SetRainBlend(1f);
                Assert.That(sun.intensity, Is.LessThan(dayIntensity));
                Assert.That(sun.intensity, Is.GreaterThan(0.8f), "Rain should stay bright enough to play.");
                Assert.That(clock.TimeOfDay, Is.EqualTo(0.5f));
                Assert.That(VillageTraderRoutine.IsNight(clock.TimeOfDay), Is.False);
                clock.SetRainBlend(0f);
                Assert.That(sun.intensity, Is.EqualTo(dayIntensity).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(lightObject);
                RenderSettings.skybox = sky;
                RenderSettings.ambientLight = ambient;
                RenderSettings.fog = fog;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogDensity = density;
            }
        }
    }
}
