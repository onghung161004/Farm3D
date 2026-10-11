using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmRestoration.World
{
    [DisallowMultipleComponent]
    public sealed class FarmWeatherVisuals : MonoBehaviour
    {
        [SerializeField] private FarmWeatherController weather;
        [SerializeField] private DayNightCycle dayNight;
        [SerializeField] private Transform followTarget;
        [SerializeField] private Material rainMaterial;

        private ParticleSystem rain;
        private GameObject rainObject;
        private GameObject statusObject;
        private TMP_Text statusText;
        private bool createdStatus;

        public void ConfigureMaterial(Material material) => rainMaterial = material;

        private void OnEnable()
        {
            if (weather == null) weather = GetComponent<FarmWeatherController>();
            if (dayNight == null) dayNight = GetComponent<DayNightCycle>();
            if (followTarget == null)
            {
                PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
                if (player != null) followTarget = player.transform;
            }
            CreateRain();
            CreateStatus();
            if (weather != null)
            {
                weather.WeatherChanged += OnWeatherChanged;
                OnWeatherChanged(weather.CurrentWeather);
            }
        }

        private void LateUpdate()
        {
            if (rainObject != null && followTarget != null)
            {
                Vector3 position = followTarget.position;
                rainObject.transform.position = new Vector3(position.x, position.y + 8f, position.z);
            }
        }

        private void OnDisable()
        {
            if (weather != null) weather.WeatherChanged -= OnWeatherChanged;
            if (rain != null) rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (dayNight != null) dayNight.SetRainBlend(0f);
        }

        private void OnDestroy()
        {
            if (rainObject != null) Destroy(rainObject);
            if (createdStatus && statusObject != null) Destroy(statusObject);
        }

        private void OnWeatherChanged(FarmWeatherKind kind)
        {
            bool raining = kind == FarmWeatherKind.Rain;
            if (dayNight != null) dayNight.SetRainBlend(raining ? 1f : 0f);
            if (statusText != null) statusText.text = raining ? "WEATHER  Rain - crops watered" : "WEATHER  Clear";
            if (rain != null)
            {
                if (raining && !rain.isPlaying) rain.Play();
                else if (!raining && rain.isPlaying) rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void CreateRain()
        {
            if (rainObject != null) return;
            rainObject = new GameObject("Local Rain VFX", typeof(ParticleSystem));
            rain = rainObject.GetComponent<ParticleSystem>();
            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = rain.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 1.1f;
            main.startSpeed = 0f;
            main.startSize = 0.05f;
            main.startColor = new Color(0.67f, 0.83f, 1f, 0.52f);
            main.maxParticles = 180;
            var emission = rain.emission;
            emission.rateOverTime = 100f;
            var shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(24f, 0.1f, 24f);
            var velocity = rain.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = -13f;
            if (rainMaterial == null)
            {
                Debug.LogWarning("Rain particle material is missing; weather gameplay remains active.", this);
                Destroy(rainObject);
                rainObject = null;
                rain = null;
                return;
            }
            ParticleSystemRenderer renderer = rain.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = rainMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 3f;
        }

        private void CreateStatus()
        {
            FarmHud hud = FindAnyObjectByType<FarmHud>();
            if (hud == null) return;
            Transform existing = hud.transform.Find("WeatherStatus");
            createdStatus = existing == null;
            statusObject = existing != null ? existing.gameObject : new GameObject("WeatherStatus", typeof(RectTransform), typeof(Image));
            if (existing == null) statusObject.transform.SetParent(hud.transform, false);
            RectTransform rect = statusObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(18f, -18f);
            rect.sizeDelta = new Vector2(370f, 42f);
            Image background = statusObject.GetComponent<Image>();
            background.color = new Color(0.06f, 0.12f, 0.16f, 0.86f);
            background.raycastTarget = false;
            statusText = statusObject.GetComponentInChildren<TMP_Text>();
            if (statusText == null)
            {
                GameObject textObject = new GameObject("WeatherText", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(statusObject.transform, false);
                statusText = textObject.GetComponent<TextMeshProUGUI>();
                RectTransform textRect = statusText.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(10f, 2f);
                textRect.offsetMax = new Vector2(-6f, -2f);
            }
            statusText.fontSize = 22f;
            statusText.color = Color.white;
            statusText.alignment = TextAlignmentOptions.MidlineLeft;
            statusText.raycastTarget = false;
        }
    }
}
