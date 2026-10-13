using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class FarmAudioSettingsTests
    {
        private GameObject root;
        private AudioVolumeState previous;
        private string oldKey, testKey;
        private static readonly FieldInfo Key = typeof(FarmAudioSettings).GetField("preferenceKey", BindingFlags.Static | BindingFlags.NonPublic);
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp()
        {
            previous = FarmAudioSettings.Snapshot(); oldKey = (string)Key.GetValue(null);
            testKey = "FarmAudioTest." + Guid.NewGuid(); Key.SetValue(null, testKey);
            FarmAudioSettings.RestoreSession(null);
            root = new GameObject("AudioTest", typeof(Canvas));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (FarmAudioSettingsView view in root.GetComponentsInChildren<FarmAudioSettingsView>(true)) Call(view, "OnDestroy");
            foreach (FarmAudioChannel channel in root.GetComponentsInChildren<FarmAudioChannel>(true)) Call(channel, "OnDestroy");
            foreach (FarmBackgroundMusic music in root.GetComponentsInChildren<FarmBackgroundMusic>(true)) Call(music, "OnDestroy");
            foreach (FishingFeedback feedback in root.GetComponentsInChildren<FishingFeedback>(true))
                foreach (string field in new[] { "splashFallback", "bite", "hook", "caught", "lost" })
                {
                    var clip = (AudioClip)typeof(FishingFeedback).GetField(field, Private).GetValue(feedback);
                    if (clip != null) UnityEngine.Object.DestroyImmediate(clip);
                }
            foreach (FarmAudioFeedback feedback in root.GetComponentsInChildren<FarmAudioFeedback>(true))
            {
                var clip = (AudioClip)typeof(FarmAudioFeedback).GetField("water", Private).GetValue(feedback);
                var original = (AudioClip)typeof(FarmAudioFeedback).GetField("waterSource", Private).GetValue(feedback);
                if (clip != null && clip != original) UnityEngine.Object.DestroyImmediate(clip);
            }
            UnityEngine.Object.DestroyImmediate(root);
            PlayerPrefs.DeleteKey(testKey); Key.SetValue(null, oldKey); FarmAudioSettings.RestoreSession(previous);
        }

        private static void Call(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);
        private AudioSource Source(string name)
        {
            var go = new GameObject(name, typeof(AudioSource)); go.transform.SetParent(root.transform);
            return go.GetComponent<AudioSource>();
        }
        private FarmHud Hud()
        {
            var go = new GameObject("Inventory"); go.transform.SetParent(root.transform); go.SetActive(false);
            return go.AddComponent<FarmHud>();
        }

        [TestCase(-1f, 0f)] [TestCase(0f, 0f)] [TestCase(0.5f, 0.5f)] [TestCase(1f, 1f)] [TestCase(100f, 1f)]
        public void VolumeBoundariesAreClamped(float input, float expected)
        {
            FarmAudioSettings.SetVolume(FarmAudioGroup.Music, input);
            Assert.That(FarmAudioSettings.Gain(FarmAudioGroup.Music), Is.EqualTo(expected));
            Assert.That(FarmAudioSettings.Gain(FarmAudioGroup.Effects), Is.EqualTo(1));
        }

        [Test]
        public void NullAndNonFiniteSettingsRetainSafeDefaults()
        {
            var normalized = AudioVolumeState.Normalize(new AudioVolumeState { music = float.NaN, effects = float.PositiveInfinity });
            Assert.That(normalized.music, Is.EqualTo(1)); Assert.That(normalized.effects, Is.EqualTo(1));
            Assert.That(AudioVolumeState.Normalize(null).Gain(FarmAudioGroup.Effects), Is.EqualTo(1));
        }

        [Test]
        public void MusicEffectsAndGlobalMutesPreserveIndependentLevels()
        {
            FarmAudioSettings.SetVolume(FarmAudioGroup.Music, 0.3f); FarmAudioSettings.SetVolume(FarmAudioGroup.Effects, 0.7f);
            FarmAudioSettings.SetMuted(FarmAudioGroup.Music, true);
            Assert.That(FarmAudioSettings.Gain(FarmAudioGroup.Music), Is.Zero);
            Assert.That(FarmAudioSettings.Gain(FarmAudioGroup.Effects), Is.EqualTo(0.7f));
            FarmAudioSettings.SetAllMuted(true);
            Assert.That(FarmAudioSettings.Gain(FarmAudioGroup.Effects), Is.Zero);
            FarmAudioSettings.SetAllMuted(false);
            Assert.That(FarmAudioSettings.Gain(FarmAudioGroup.Music), Is.Zero, "Group mute remains after global unmute.");
            FarmAudioSettings.SetMuted(FarmAudioGroup.Music, false);
            Assert.That(FarmAudioSettings.Gain(FarmAudioGroup.Music), Is.EqualTo(0.3f));
            var copy = FarmAudioSettings.Snapshot(); copy.music = 0;
            Assert.That(FarmAudioSettings.Snapshot().music, Is.EqualTo(0.3f));
        }

        [Test]
        public void BoundChannelsUpdateImmediatelyWithoutAffectingOtherGroupAndBindIsIdempotent()
        {
            AudioSource music = Source("Music"), effect = Source("Bò α");
            FarmAudioChannel channel = FarmAudioChannel.Bind(music, FarmAudioGroup.Music, 0.18f);
            FarmAudioChannel.Bind(effect, FarmAudioGroup.Effects, 0.75f);
            FarmAudioSettings.SetVolume(FarmAudioGroup.Music, 0.5f);
            Assert.That(music.volume, Is.EqualTo(0.09f).Within(0.0001)); Assert.That(effect.volume, Is.EqualTo(0.75f));
            FarmAudioSettings.SetVolume(FarmAudioGroup.Effects, 0.2f);
            Assert.That(effect.volume, Is.EqualTo(0.15f).Within(0.0001));
            Assert.That(FarmAudioChannel.Bind(music, FarmAudioGroup.Music, 0.18f), Is.SameAs(channel));
            Assert.That(music.GetComponents<FarmAudioChannel>().Length, Is.EqualTo(1));
            FarmAudioSettings.SetAllMuted(true);
            channel.SetBaseVolume(0.1f); // Music duck/fade must not bypass mute.
            Assert.That(music.volume, Is.Zero); Assert.That(effect.volume, Is.Zero);
            FarmAudioSettings.SetAllMuted(false);
            Assert.That(music.volume, Is.EqualTo(0.05f).Within(0.0001));
        }

        [Test]
        public void NewSourcesInheritMuteAndUnsubscribedChannelStopsReceivingChanges()
        {
            FarmAudioSettings.SetAllMuted(true);
            AudioSource effect = Source("New sound");
            var channel = FarmAudioChannel.Bind(effect, FarmAudioGroup.Effects, 0.7f);
            Assert.That(effect.volume, Is.Zero);
            Call(channel, "OnDisable"); FarmAudioSettings.SetAllMuted(false);
            Assert.That(effect.volume, Is.Zero);
            Call(channel, "OnEnable"); Assert.That(effect.volume, Is.EqualTo(0.7f));
            Assert.That(FarmAudioChannel.Bind(null, FarmAudioGroup.Effects, 1), Is.Null);
        }

        [Test]
        public void RoundTripPersistsWithoutWritingOnEverySliderChangeAndCorruptDataFallsBack()
        {
            FarmAudioSettings.SetVolume(FarmAudioGroup.Music, 0.27f);
            FarmAudioSettings.SetVolume(FarmAudioGroup.Effects, 0.61f); FarmAudioSettings.SetMuted(FarmAudioGroup.Effects, true);
            Assert.That(PlayerPrefs.HasKey(testKey), Is.False);
            FarmAudioSettings.Save(); FarmAudioSettings.Reload();
            Assert.That(FarmAudioSettings.Snapshot().music, Is.EqualTo(0.27f));
            Assert.That(FarmAudioSettings.Snapshot().effects, Is.EqualTo(0.61f));
            Assert.That(FarmAudioSettings.Gain(FarmAudioGroup.Effects), Is.Zero);
            PlayerPrefs.SetString(testKey, "not json"); FarmAudioSettings.Reload();
            Assert.That(FarmAudioSettings.Snapshot().music, Is.EqualTo(1));
        }

        [Test]
        public void PopupSlidersUseNoNotifyRefreshAndBlockGameplayWhileOwningOnlyItsLock()
        {
            var obj = new GameObject("Player"); obj.transform.SetParent(root.transform);
            PlayerMovement player = obj.AddComponent<PlayerMovement>(); var tools = obj.AddComponent<PlayerToolController>();
            FarmHud hud = Hud(); FarmAudioSettingsView.Ensure(hud); FarmAudioSettingsView.Ensure(hud);
            Assert.That(root.GetComponentsInChildren<FarmAudioSettingsView>(true).Length, Is.EqualTo(1));
            Assert.That(FarmAudioSettingsView.Show(), Is.True); Assert.That(player.ControlsLocked, Is.True);
            var view = root.GetComponentInChildren<FarmAudioSettingsView>();
            object slider = typeof(FarmAudioSettingsView).GetField("music", Private).GetValue(view);
            slider.GetType().GetProperty("value").SetValue(slider, 0.4f);
            Assert.That(FarmAudioSettings.Gain(FarmAudioGroup.Music), Is.EqualTo(0.4f));
            tools.SelectTool(8); Assert.That(tools.SelectedTool, Is.EqualTo(FarmTool.Hoe));
            Assert.That(tools.TryUseTool(), Is.False); Assert.That(tools.TryCycleRecipe(), Is.False);
            view.Close(); Assert.That(player.ControlsLocked, Is.False); Assert.That(FarmAudioSettingsView.IsOpen, Is.False);
            player.SetControlsLocked(true); FarmAudioSettingsView.Show(); view.Close(); Assert.That(player.ControlsLocked, Is.True);
        }

        [Test]
        public void ActualMusicAndTreeFallSourcesUseDifferentChannels()
        {
            FarmAudioSettings.SetVolume(FarmAudioGroup.Music, 0.5f); FarmAudioSettings.SetVolume(FarmAudioGroup.Effects, 0.2f);
            FarmBackgroundMusic music = FarmBackgroundMusic.Ensure(Hud());
            music.GetComponent<FarmAudioChannel>().SetBaseVolume(0.18f);
            var go = new GameObject("Tree"); go.transform.SetParent(root.transform);
            TreeFallAudio.Ensure(go.AddComponent<ChoppableTree>());
            Assert.That(music.GetComponent<AudioSource>().volume, Is.EqualTo(0.09f).Within(0.0001));
            Assert.That(go.GetComponent<AudioSource>().volume, Is.EqualTo(0.15f).Within(0.0001));
            FarmAudioSettings.SetMuted(FarmAudioGroup.Effects, true);
            Assert.That(go.GetComponent<AudioSource>().volume, Is.Zero);
            Assert.That(music.GetComponent<AudioSource>().volume, Is.EqualTo(0.09f).Within(0.0001));
        }

        [Test]
        public void PartialPreferencesKeepMissingGroupAtDefaultAndClampInvalidNumbers()
        {
            PlayerPrefs.SetString(testKey, "{\"music\":0.25}"); FarmAudioSettings.Reload();
            Assert.That(FarmAudioSettings.Snapshot().music, Is.EqualTo(0.25f));
            Assert.That(FarmAudioSettings.Snapshot().effects, Is.EqualTo(1));
            PlayerPrefs.SetString(testKey, "{\"music\":-4,\"effects\":18}"); FarmAudioSettings.Reload();
            Assert.That(FarmAudioSettings.Snapshot().music, Is.Zero);
            Assert.That(FarmAudioSettings.Snapshot().effects, Is.EqualTo(1));
        }

        [Test]
        public void ActualFootstepsActionsCowAndFishingAllFollowEffectsGain()
        {
            FarmAudioSettings.SetVolume(FarmAudioGroup.Effects, 0.2f);
            GameObject Actor(string name) { var go = new GameObject(name); go.transform.SetParent(root.transform); return go; }
            var farming = Actor("Player SFX").AddComponent<FarmAudioFeedback>(); Call(farming, "Awake");
            var foot = (AudioSource)typeof(FarmAudioFeedback).GetField("footstepSource", Private).GetValue(farming);
            var action = (AudioSource)typeof(FarmAudioFeedback).GetField("actionSource", Private).GetValue(farming);
            Assert.That(foot.volume, Is.EqualTo(0.42f * 0.2f).Within(0.0001));
            Assert.That(action.volume, Is.EqualTo(0.2f).Within(0.0001));
            var cow = Actor("Cow SFX").AddComponent<CowAudioFeedback>(); Call(cow, "Awake");
            var fishing = Actor("Fishing SFX").AddComponent<FishingFeedback>(); Call(fishing, "Awake");
            Assert.That(cow.GetComponent<AudioSource>().volume, Is.EqualTo(0.75f * 0.2f).Within(0.0001));
            Assert.That(fishing.GetComponent<AudioSource>().volume, Is.EqualTo(0.7f * 0.2f).Within(0.0001));
            FarmAudioSettings.SetMuted(FarmAudioGroup.Effects, true);
            Assert.That(foot.volume + action.volume + cow.GetComponent<AudioSource>().volume + fishing.GetComponent<AudioSource>().volume, Is.Zero);
        }
    }
}
