using System;
using UnityEngine;

namespace FarmRestoration
{
    public static class FarmAudioSettings
    {
        private static string preferenceKey = "FarmRestoration.Audio.v1";
        private static AudioVolumeState state = new AudioVolumeState();
        private static bool loaded, dirty;
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { loaded = dirty = false; Changed = null; state = new AudioVolumeState(); }

        public static AudioVolumeState Snapshot() { EnsureLoaded(); return AudioVolumeState.Normalize(state); }
        public static float Gain(FarmAudioGroup group) { EnsureLoaded(); return state.Gain(group); }

        private static void EnsureLoaded() { if (!loaded) Reload(); }

        public static void Reload()
        {
            state = new AudioVolumeState(); loaded = true; dirty = false;
            string json = PlayerPrefs.GetString(preferenceKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var saved = new AudioVolumeState();
                    JsonUtility.FromJsonOverwrite(json, saved);
                    state = AudioVolumeState.Normalize(saved);
                }
                catch (ArgumentException) { /* Invalid device preference; retain safe defaults. */ }
            }
            Changed?.Invoke();
        }

        public static void RestoreSession(AudioVolumeState value)
        { state = AudioVolumeState.Normalize(value); loaded = true; dirty = false; Changed?.Invoke(); }

        public static void SetVolume(FarmAudioGroup group, float value)
        {
            EnsureLoaded(); value = AudioVolumeState.SafeVolume(value);
            if (group == FarmAudioGroup.Music) { if (state.music == value) return; state.music = value; }
            else { if (state.effects == value) return; state.effects = value; }
            Publish();
        }

        public static void SetMuted(FarmAudioGroup group, bool muted)
        {
            EnsureLoaded();
            if (group == FarmAudioGroup.Music) { if (state.musicMuted == muted) return; state.musicMuted = muted; }
            else { if (state.effectsMuted == muted) return; state.effectsMuted = muted; }
            Publish();
        }

        public static void SetAllMuted(bool muted)
        { EnsureLoaded(); if (state.allMuted == muted) return; state.allMuted = muted; Publish(); }
        private static void Publish() { dirty = true; Changed?.Invoke(); }

        public static void Save()
        {
            if (!loaded || !dirty) return;
            PlayerPrefs.SetString(preferenceKey, JsonUtility.ToJson(state));
            PlayerPrefs.Save(); dirty = false;
        }
    }
}
