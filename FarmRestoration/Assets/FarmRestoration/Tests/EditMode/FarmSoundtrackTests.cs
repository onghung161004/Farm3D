using System;
using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class FarmSoundtrackTests
    {
        [Test]
        public void RecordedWoodHitAndFallAreIncludedAndSeparate()
        {
            AudioClip hit = Resources.Load<AudioClip>("FarmAudio/wood_hit_01");
            AudioClip fall = Resources.Load<AudioClip>("FarmAudio/tree_fall");
            Assert.That(hit, Is.Not.Null);
            Assert.That(fall, Is.Not.Null);
            Assert.That(hit.length, Is.InRange(0.40f, 0.55f));
            Assert.That(fall.length, Is.InRange(1.90f, 2.00f));
            Assert.That(hit.channels, Is.EqualTo(1));
            Assert.That(fall.channels, Is.EqualTo(1));
        }

        [Test]
        public void OriginalMusicIsStereoAudibleBoundedAndHasTaperedLoopEnds()
        {
            float[] samples = FarmSoundGenerator.BuildMusicSamples(8000);
            Assert.That(samples.Length, Is.EqualTo(8000 * 32 * 2));
            Assert.That(samples[0], Is.Zero);
            Assert.That(samples[samples.Length - 1], Is.Zero);
            double energy = 0;
            bool stereoDifference = false;
            for (int i = 0; i < samples.Length; i++)
            {
                Assert.That(float.IsNaN(samples[i]) || float.IsInfinity(samples[i]), Is.False);
                Assert.That(Mathf.Abs(samples[i]), Is.LessThanOrEqualTo(0.8f));
                energy += samples[i] * samples[i];
                if (i % 2 == 0 && Mathf.Abs(samples[i] - samples[i + 1]) > 0.01f) stereoDifference = true;
            }
            Assert.That(energy / samples.Length, Is.GreaterThan(0.0001));
            Assert.That(stereoDifference, Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => FarmSoundGenerator.BuildMusicSamples(0));
        }

        [Test]
        public void TreeFallImpactMatchesAnimationEndAndIsNotSilenceOrClipped()
        {
            const int rate = 8000;
            float[] samples = FarmSoundGenerator.BuildFallSamples(rate, TreeHarvestVisual.FallDuration);
            Assert.That(samples.Length, Is.EqualTo((int)(rate * FarmSoundGenerator.FallSeconds)));
            Assert.That(samples[0], Is.Zero);
            Assert.That(Mathf.Abs(samples[samples.Length - 1]), Is.LessThan(0.001f));
            float peak = 0f;
            foreach (float value in samples) peak = Mathf.Max(peak, Mathf.Abs(value));
            Assert.That(peak, Is.InRange(0.3f, 0.85f));
            float impact = TreeHarvestVisual.FallDuration;
            Assert.That(Energy(impact, impact + 0.20f), Is.GreaterThan(Energy(impact - 0.20f, impact - 0.05f) * 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => FarmSoundGenerator.BuildFallSamples(-1, 1f));

            double Energy(float from, float to)
            {
                double result = 0;
                int start = (int)(from * rate), end = (int)(to * rate);
                for (int i = start; i < end; i++) result += samples[i] * samples[i];
                return result / (end - start);
            }
        }

        [Test]
        public void BackgroundInstallerCreatesOneQuietLoopAndTreeSourceIsSpatial()
        {
            GameObject hudObject = new GameObject("MusicHud");
            GameObject treeObject = new GameObject("SoundTree");
            try
            {
                hudObject.SetActive(false);
                FarmHud hud = hudObject.AddComponent<FarmHud>();
                FarmBackgroundMusic music = FarmBackgroundMusic.Ensure(hud);
                Assert.That(FarmBackgroundMusic.Ensure(hud), Is.SameAs(music));
                AudioSource background = music.GetComponent<AudioSource>();
                Assert.That(background.clip, Is.Not.Null);
                Assert.That(background.clip.channels, Is.EqualTo(2));
                Assert.That(background.loop, Is.True);
                Assert.That(background.spatialBlend, Is.Zero);
                Assert.That(background.volume, Is.Zero, "Music starts faded out.");
                Assert.That(hudObject.GetComponentsInChildren<AudioSource>(true).Length, Is.EqualTo(1));
                ChoppableTree tree = treeObject.AddComponent<ChoppableTree>();
                TreeFallAudio audio = TreeFallAudio.Ensure(tree);
                Assert.That(TreeFallAudio.Ensure(tree), Is.SameAs(audio));
                AudioSource fall = tree.GetComponent<AudioSource>();
                Assert.That(fall.clip, Is.Not.Null);
                Assert.That(fall.spatialBlend, Is.EqualTo(1f));
                Assert.That(fall.maxDistance, Is.EqualTo(28f));
                Assert.That(fall.playOnAwake, Is.False);
                Assert.That(fall.clip, Is.SameAs(Resources.Load<AudioClip>("FarmAudio/tree_fall")));
                Assert.That(fall.pitch, Is.EqualTo(0.75f));
                Assert.That(tree.GetComponents<AudioSource>().Length, Is.EqualTo(1));
                Assert.That(TreeFallAudio.Ensure(null), Is.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(hudObject); UnityEngine.Object.DestroyImmediate(treeObject); }
        }
    }
}
