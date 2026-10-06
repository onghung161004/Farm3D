using UnityEngine;

namespace FarmRestoration.World
{
    /// <summary>Drives FarmDemo's directional light and global environment through a gentle day/night loop.</summary>
    [DisallowMultipleComponent]
    public sealed class DayNightCycle : MonoBehaviour
    {
        [SerializeField, Min(30f)] private float cycleDurationSeconds = 180f;
        [SerializeField, Range(0f, 1f)] private float timeOfDay = 0.30f;
        [SerializeField] private Light sun;

        private static readonly Color DaySun = new Color(1.00f, 0.91f, 0.72f);
        private static readonly Color NightSun = new Color(0.25f, 0.37f, 0.62f);
        private static readonly Color DayAmbient = new Color(0.72f, 0.84f, 0.76f);
        private static readonly Color NightAmbient = new Color(0.08f, 0.12f, 0.22f);
        private static readonly Color DayFog = new Color(0.70f, 0.86f, 0.92f);
        private static readonly Color NightFog = new Color(0.07f, 0.11f, 0.21f);

        private void Awake()
        {
            if (sun == null) sun = GetComponent<Light>();
            if (sun == null) enabled = false;
        }

        private void Update()
        {
            timeOfDay = Mathf.Repeat(timeOfDay + Time.deltaTime / cycleDurationSeconds, 1f);
            ApplyEnvironment();
        }

        private void OnEnable()
        {
            if (sun == null) sun = GetComponent<Light>();
            if (sun != null) ApplyEnvironment();
        }

        private void ApplyEnvironment()
        {
            float altitude = Mathf.Sin((timeOfDay - 0.25f) * Mathf.PI * 2f);
            float daylight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.16f, 0.20f, altitude));
            sun.transform.rotation = Quaternion.Euler((timeOfDay * 360f) - 90f, 145f, 0f);
            sun.color = Color.Lerp(NightSun, DaySun, daylight);
            sun.intensity = Mathf.Lerp(0.18f, 1.18f, daylight);

            RenderSettings.ambientLight = Color.Lerp(NightAmbient, DayAmbient, daylight);
            RenderSettings.fog = true;
            RenderSettings.fogColor = Color.Lerp(NightFog, DayFog, daylight);
            RenderSettings.fogDensity = Mathf.Lerp(0.014f, 0.0055f, daylight);

            Material skybox = RenderSettings.skybox;
            if (skybox != null && skybox.HasProperty("_Exposure"))
                skybox.SetFloat("_Exposure", Mathf.Lerp(0.32f, 1.22f, daylight));
        }
    }
}
