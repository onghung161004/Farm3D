using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class FishingFeedback : MonoBehaviour
    {
        private AudioSource source;
        private AudioClip[] splashes;
        private AudioClip splashFallback;
        private AudioClip bite;
        private AudioClip hook;
        private AudioClip caught;
        private AudioClip lost;

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0.2f;
            FarmAudioChannel.Bind(source, FarmAudioGroup.Effects, 0.7f);
            splashes = new[]
            {
                Resources.Load<AudioClip>("FarmAudio/splash_09"),
                Resources.Load<AudioClip>("FarmAudio/splash_10")
            };
            if (splashes[0] == null && splashes[1] == null)
            {
                Debug.LogWarning("Fishing splash clips are missing from Resources/FarmAudio.", this);
                splashFallback = Tone("Fishing splash fallback", 180f, 0.28f, true);
            }
            bite = Tone("Fishing bite", 850f, 0.28f, false);
            hook = Tone("Fishing hook", 420f, 0.14f, false);
            caught = Tone("Fish caught", 1050f, 0.45f, false);
            lost = Tone("Fish escaped", 300f, 0.30f, false);
        }

        public void PlayCast()
        {
            AudioClip first = splashes[Random.Range(0, splashes.Length)];
            Play(first != null ? first : splashes[0] ?? splashes[1] ?? splashFallback);
        }
        public void PlayHook() => Play(hook);
        public void PlayCaught() => Play(caught);
        public void PlayLost() => Play(lost);

        public void PlayBite()
        {
            Play(bite);
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        private void Play(AudioClip clip)
        {
            if (source != null && clip != null) source.PlayOneShot(clip);
        }

        private static AudioClip Tone(string name, float frequency, float seconds, bool noise)
        {
            const int sampleRate = 22050;
            int count = Mathf.CeilToInt(sampleRate * seconds);
            float[] samples = new float[count];
            uint seed = 1937;
            float filtered = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * i / count);
                if (noise)
                {
                    seed = seed * 1664525u + 1013904223u;
                    float white = ((seed >> 8) / 16777216f) * 2f - 1f;
                    filtered = Mathf.Lerp(filtered, white, 0.08f);
                    samples[i] = filtered * envelope * 0.32f;
                }
                else samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.22f;
            }
            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (splashFallback != null) Destroy(splashFallback);
            if (bite != null) Destroy(bite);
            if (hook != null) Destroy(hook);
            if (caught != null) Destroy(caught);
            if (lost != null) Destroy(lost);
        }
    }
}
