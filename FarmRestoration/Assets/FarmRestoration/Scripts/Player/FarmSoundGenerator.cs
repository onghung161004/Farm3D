using System;
using UnityEngine;

namespace FarmRestoration
{
    /// <summary>Original gentle farm melody and wood/leaf fall sound; no third-party media required.</summary>
    public static class FarmSoundGenerator
    {
        public const int SampleRate = 22050;
        public const float MusicSeconds = 32f;
        public const float FallSeconds = 2.2f;

        public static AudioClip CreateMusic()
        {
            AudioClip clip = AudioClip.Create("Farm - original pastoral loop", (int)(SampleRate * MusicSeconds), 2, SampleRate, false);
            clip.SetData(BuildMusicSamples(SampleRate), 0);
            return clip;
        }

        public static AudioClip CreateTreeFall()
        {
            AudioClip clip = AudioClip.Create("Tree - crack leaves and ground impact", (int)(SampleRate * FallSeconds), 1, SampleRate, false);
            clip.SetData(BuildFallSamples(SampleRate, TreeHarvestVisual.FallDuration), 0);
            return clip;
        }

        public static float[] BuildMusicSamples(int sampleRate)
        {
            ValidateRate(sampleRate);
            int frames = (int)(sampleRate * MusicSeconds);
            float[] samples = new float[frames * 2];
            int[][] chords =
            {
                new[] { 48, 52, 55 }, new[] { 45, 48, 52 }, new[] { 41, 45, 48 }, new[] { 43, 47, 50 },
                new[] { 41, 45, 48 }, new[] { 48, 52, 55 }, new[] { 38, 41, 45 }, new[] { 43, 47, 50 }
            };
            int[] melody = { 72, 76, 79, 76, 72, 69, 72, 76, 77, 76, 72, 69, 71, 74, 79, 74,
                77, 81, 79, 77, 76, 72, 67, 72, 74, 77, 76, 74, 71, 74, 67, 71 };
            for (int bar = 0; bar < chords.Length; bar++)
            {
                float start = bar * 4f; // 60 BPM, eight four-beat bars.
                AddNote(chords[bar][0] - 12, start, 3.7f, 0.11f, 0f, false);
                for (int beat = 0; beat < 4; beat++)
                {
                    AddNote(chords[bar][beat % 3] + 12, start + beat, 1.8f, 0.09f, -0.30f, true);
                    AddNote(melody[bar * 4 + beat], start + beat + 0.25f, 1.6f, 0.12f, 0.30f, true);
                }
            }
            for (int frame = 0; frame < frames; frame++)
            {
                float fade = Mathf.Min(1f, Mathf.Min(frame / (sampleRate * 0.04f), (frames - 1 - frame) / (sampleRate * 0.12f)));
                samples[frame * 2] = Mathf.Clamp(samples[frame * 2] * fade, -0.8f, 0.8f);
                samples[frame * 2 + 1] = Mathf.Clamp(samples[frame * 2 + 1] * fade, -0.8f, 0.8f);
            }
            return samples;

            void AddNote(int midi, float onset, float duration, float gain, float pan, bool plucked)
            {
                double frequency = 440d * Math.Pow(2d, (midi - 69) / 12d);
                int first = Mathf.RoundToInt(onset * sampleRate);
                int length = Mathf.RoundToInt(duration * sampleRate);
                for (int i = 0; i < length; i++)
                {
                    int frame = (first + i) % frames; // Release tails wrap into the start of the loop.
                    float t = i / (float)sampleRate;
                    double phase = 2d * Math.PI * frequency * t;
                    float harmonics = (float)(Math.Sin(phase) + 0.22d * Math.Sin(phase * 2d)
                        + (plucked ? 0.08d * Math.Sin(phase * 3d) : 0d));
                    float envelope = Mathf.Min(1f, t / 0.025f) * Mathf.Exp(-t * (plucked ? 2.8f : 1.0f))
                        * Mathf.Clamp01((duration - t) / 0.2f);
                    float value = harmonics * envelope * gain;
                    samples[frame * 2] += value * (1f - pan) * 0.7f;
                    samples[frame * 2 + 1] += value * (1f + pan) * 0.7f;
                }
            }
        }

        public static float[] BuildFallSamples(int sampleRate, float impactAt)
        {
            ValidateRate(sampleRate);
            impactAt = Mathf.Clamp(impactAt, 0.2f, FallSeconds - 0.5f);
            int frames = (int)(sampleRate * FallSeconds);
            float[] samples = new float[frames];
            uint seed = 9127;
            float filtered = 0f;
            for (int i = 0; i < frames; i++)
            {
                seed = seed * 1664525u + 1013904223u;
                float noise = ((seed >> 8) / 16777216f) * 2f - 1f;
                filtered = Mathf.Lerp(filtered, noise, 0.045f);
                float t = i / (float)sampleRate;
                float crack = Pulse(t, 0.12f, 0.045f) + 0.6f * Pulse(t, 0.34f, 0.06f);
                float rustle = Mathf.Sin(Mathf.Clamp01(t / impactAt) * Mathf.PI) * 0.18f;
                float impactTime = t - impactAt;
                float thud = impactTime < 0f ? 0f : Mathf.Exp(-impactTime * 8f)
                    * (Mathf.Sin(2f * Mathf.PI * 65f * impactTime) * 0.48f + filtered * 1.7f);
                float fade = Mathf.Min(1f, Mathf.Min(t / 0.01f, (FallSeconds - t) / 0.08f));
                samples[i] = Mathf.Clamp((noise * (crack * 0.55f + rustle) + thud) * fade, -0.85f, 0.85f);
            }
            return samples;
        }

        private static float Pulse(float t, float start, float decay)
            => t < start ? 0f : Mathf.Exp(-(t - start) / decay);

        private static void ValidateRate(int rate)
        {
            if (rate < 8000 || rate > 48000) throw new ArgumentOutOfRangeException(nameof(rate));
        }
    }
}
