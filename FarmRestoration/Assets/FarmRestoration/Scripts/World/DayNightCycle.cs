using UnityEngine;

namespace FarmRestoration.World
{
    /// <summary>Keeps FarmDemo in daylight while retaining the weather and NPC clock interface.</summary>
    [DisallowMultipleComponent]
    public sealed class DayNightCycle : MonoBehaviour
    {
        [SerializeField] private Light sun;
        private float rainBlend;

        public float TimeOfDay => 0.5f;

        private static readonly Color DaySun = new Color(1.00f, 0.91f, 0.72f);
        private static readonly Color DayAmbient = new Color(0.72f, 0.84f, 0.76f);
        private static readonly Color DayFog = new Color(0.70f, 0.86f, 0.92f);

        private void Awake()
        {
            if (sun == null) sun = GetComponent<Light>();
            if (sun == null) enabled = false;
        }

        private void OnEnable()
        {
            if (sun == null) sun = GetComponent<Light>();
            if (sun != null) ApplyEnvironment();
        }

        public void SetRainBlend(float value)
        {
            rainBlend = Mathf.Clamp01(value);
            if (sun == null) sun = GetComponent<Light>();
            if (sun != null) ApplyEnvironment();
        }

        private void ApplyEnvironment()
        {
            sun.transform.rotation = Quaternion.Euler(65f, 145f, 0f);
            sun.color = Color.Lerp(DaySun, new Color(0.58f, 0.67f, 0.76f), rainBlend * 0.45f);
            sun.intensity = 1.18f * Mathf.Lerp(1f, 0.7f, rainBlend);

            RenderSettings.ambientLight = Color.Lerp(DayAmbient, DayAmbient * 0.78f, rainBlend);
            RenderSettings.fog = true;
            RenderSettings.fogColor = Color.Lerp(DayFog, DayFog * new Color(0.72f, 0.8f, 0.9f), rainBlend);
            RenderSettings.fogDensity = 0.0055f + rainBlend * 0.0015f;

            Material skybox = RenderSettings.skybox;
            if (skybox != null && skybox.HasProperty("_Exposure"))
                skybox.SetFloat("_Exposure", 1.22f * Mathf.Lerp(1f, 0.8f, rainBlend));
        }
    }
}
