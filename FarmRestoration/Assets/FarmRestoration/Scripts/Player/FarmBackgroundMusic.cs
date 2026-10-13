using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class FarmBackgroundMusic : MonoBehaviour
    {
        private static FarmBackgroundMusic instance;
        private AudioSource source;
        private AudioClip ownedClip;
        private FarmAudioChannel channel;
        private float baseVolume;

        public static FarmBackgroundMusic Ensure(FarmHud hud)
        {
            if (hud == null) return null;
            if (instance != null) return instance;
            GameObject root = new GameObject("Farm Background Music");
            root.transform.SetParent(hud.transform, false);
            instance = root.AddComponent<FarmBackgroundMusic>();
            instance.Build();
            return instance;
        }

        private void Build()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.priority = 160;
            source.volume = 0f;
            channel = FarmAudioChannel.Bind(source, FarmAudioGroup.Music, 0f);
            source.clip = Resources.Load<AudioClip>("FarmAudio/farm_music");
            if (source.clip == null) source.clip = ownedClip = FarmSoundGenerator.CreateMusic();
            if (Application.isPlaying) source.Play();
        }

        private void Update()
        {
            if (source == null) return;
            float target = FishingSession.IsFishing ? 0.10f : 0.18f;
            baseVolume = Mathf.MoveTowards(baseVolume, target, Time.unscaledDeltaTime * 0.09f);
            channel.SetBaseVolume(baseVolume);
        }

        private void OnEnable()
        {
            if (source != null && Application.isPlaying) source.Play();
        }

        private void OnDisable()
        {
            if (source != null) source.Stop();
        }

        private void OnApplicationPause(bool paused)
        {
            if (source == null) return;
            if (paused) source.Pause();
            else if (isActiveAndEnabled) source.UnPause();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            if (ownedClip == null) return;
            if (Application.isPlaying) Destroy(ownedClip);
            else DestroyImmediate(ownedClip);
        }
    }
}
