using UnityEngine;

namespace FarmRestoration
{
    public sealed class FarmAudioChannel : MonoBehaviour
    {
        private AudioSource source;
        private FarmAudioGroup group;
        private float baseVolume;

        public static FarmAudioChannel Bind(AudioSource source, FarmAudioGroup group, float baseVolume)
        {
            if (source == null) return null;
            foreach (FarmAudioChannel channel in source.GetComponents<FarmAudioChannel>())
                if (channel.source == source) { channel.group = group; channel.SetBaseVolume(baseVolume); return channel; }
            FarmAudioChannel result = source.gameObject.AddComponent<FarmAudioChannel>();
            result.source = source; result.group = group;
            // Subscribe explicitly too: inactive sources receive the current setting before their first sound.
            result.Subscribe(); result.SetBaseVolume(baseVolume);
            return result;
        }

        public void SetBaseVolume(float value) { baseVolume = AudioVolumeState.SafeVolume(value); Apply(); }
        private void Apply() { if (source != null) source.volume = baseVolume * FarmAudioSettings.Gain(group); }
        private void Subscribe() { FarmAudioSettings.Changed -= Apply; FarmAudioSettings.Changed += Apply; }
        private void OnEnable() { Subscribe(); Apply(); }
        private void OnDisable() { FarmAudioSettings.Changed -= Apply; }
        private void OnDestroy() { FarmAudioSettings.Changed -= Apply; }
    }
}
