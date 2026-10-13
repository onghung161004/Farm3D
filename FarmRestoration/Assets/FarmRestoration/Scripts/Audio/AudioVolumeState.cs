using System;
using UnityEngine;

namespace FarmRestoration
{
    public enum FarmAudioGroup { Music, Effects }

    [Serializable]
    public sealed class AudioVolumeState
    {
        public float music = 1f;
        public float effects = 1f;
        public bool musicMuted, effectsMuted, allMuted;
        public float Gain(FarmAudioGroup group) => allMuted ? 0f : group == FarmAudioGroup.Music
            ? musicMuted ? 0f : music : effectsMuted ? 0f : effects;
        public static float SafeVolume(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp01(value);
        public static AudioVolumeState Normalize(AudioVolumeState saved) => saved == null ? new AudioVolumeState()
            : new AudioVolumeState { music = SafeVolume(saved.music), effects = SafeVolume(saved.effects),
                musicMuted = saved.musicMuted, effectsMuted = saved.effectsMuted, allMuted = saved.allMuted };
    }
}
