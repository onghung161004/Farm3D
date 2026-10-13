using UnityEngine;

namespace FarmRestoration
{
    /// <summary>Spatial mooing for cow interactions and occasional nearby ambience.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CowAnimal))]
    public sealed class CowAudioFeedback : MonoBehaviour
    {
        private static float lastHerdMooTime = float.NegativeInfinity;
        private static bool warnedMissingClip;
        private CowAnimal cow;
        private AudioSource source;
        private AudioClip moo;
        private Transform listener;
        private float nextAmbientTime;
        private float lastMooTime = float.NegativeInfinity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSharedState()
        {
            lastHerdMooTime = float.NegativeInfinity;
            warnedMissingClip = false;
        }

        private void Awake()
        {
            cow = GetComponent<CowAnimal>();
            moo = Resources.Load<AudioClip>("FarmAudio/cow_moo");
            if (moo == null && !warnedMissingClip)
            {
                warnedMissingClip = true;
                Debug.LogWarning("Cow sound is missing from Resources/FarmAudio.", this);
            }
            source = GetComponent<AudioSource>();
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 2f;
            source.maxDistance = 15f;
            source.volume = 0.75f;
            nextAmbientTime = Time.time + Random.Range(4f, 15f);
        }

        private void OnEnable()
        {
            if (cow != null) cow.StateChanged += OnCowStateChanged;
        }

        private void OnDisable()
        {
            if (cow != null) cow.StateChanged -= OnCowStateChanged;
        }

        private void Update()
        {
            if (moo == null || cow == null || cow.IsPenned || cow.IsFollowing
                || Time.time < nextAmbientTime) return;
            if (listener == null)
            {
                Camera camera = Camera.main;
                if (camera != null) listener = camera.transform;
            }
            if (listener == null || (listener.position - transform.position).sqrMagnitude > 15f * 15f)
            {
                nextAmbientTime = Time.time + 2f;
                return;
            }
            nextAmbientTime = Time.time + (PlayMoo(false) ? Random.Range(14f, 17f) : 1.5f);
        }

        private void OnCowStateChanged(CowAnimal changed)
        {
            PlayMoo(true);
            nextAmbientTime = Time.time + Random.Range(14f, 17f);
        }

        private bool PlayMoo(bool interaction)
        {
            if (moo == null || source == null || source.isPlaying) return false;
            float localCooldown = interaction ? 2f : 9f;
            float herdCooldown = interaction ? 0.8f : 5f;
            if (Time.time - lastMooTime < localCooldown || Time.time - lastHerdMooTime < herdCooldown) return false;
            source.pitch = Random.Range(0.94f, 1.06f);
            source.PlayOneShot(moo);
            lastMooTime = lastHerdMooTime = Time.time;
            return true;
        }
    }
}
