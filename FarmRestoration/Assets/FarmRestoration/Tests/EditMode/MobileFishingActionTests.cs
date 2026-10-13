using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class MobileFishingActionTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject root;
        private FishingHud hud;
        private FishingPondSpot spot;
        private FishingSession session;
        private PlayerToolController tools;
        private FarmProgression previousGame;
        private FieldInfo instanceField;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("MobileFishingTest", typeof(Canvas));
            GameObject player = new GameObject("Player", typeof(PlayerMovement), typeof(PlayerToolController));
            player.transform.SetParent(root.transform);
            tools = player.GetComponent<PlayerToolController>();
            instanceField = typeof(FarmProgression).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            previousGame = FarmProgression.Instance;
            GameObject gameObject = new GameObject("Progression");
            gameObject.transform.SetParent(root.transform);
            gameObject.SetActive(false);
            instanceField.SetValue(null, gameObject.AddComponent<FarmProgression>());
            GameObject point = new GameObject("RiverFishingSpot");
            point.transform.SetParent(root.transform);
            spot = point.AddComponent<FishingPondSpot>();
            session = point.GetComponent<FishingSession>();
            Set(spot, "session", session);
            Set(spot, "player", tools);
            Set(spot, "riverSpot", true);
            Set(spot, "riverWaterTarget", new Vector3(4f, 0f, 7f));
            hud = FishingHud.Ensure();
            Assert.That(hud, Is.Not.Null);
        }

        [TearDown]
        public void TearDown()
        {
            // EditMode cannot defer Destroy; release generated visual resources explicitly before Cancel.
            if (session != null)
            {
                FishingVisuals visual = Get<FishingVisuals>(session, "visuals");
                if (visual != null)
                {
                    foreach (string field in new[] { "rodMaterial", "floatMaterial", "lineMaterial" })
                    {
                        Material material = Get<Material>(visual, field);
                        Set(visual, field, null);
                        if (material != null) Object.DestroyImmediate(material);
                    }
                    Set(session, "visuals", null);
                    Object.DestroyImmediate(visual.gameObject);
                }
                session.Cancel();
            }
            Object.DestroyImmediate(root);
            instanceField.SetValue(null, previousGame);
        }

        private static void Set(object owner, string name, object value)
            => owner.GetType().GetField(name, Private).SetValue(owner, value);
        private static T Get<T>(object owner, string name)
            => (T)owner.GetType().GetField(name, Private).GetValue(owner);

        [Test]
        public void ActionCastsShownSpotWithoutGeneralRaycastAndDoesNotCastTwice()
        {
            var query = new CountingQuery();
            tools.ConfigureTargetQuery(query);
            hud.SetSpot(spot, true);
            tools.SetMobileInteractHeld(true);
            Assert.That(FishingSession.Active, Is.SameAs(session));
            Assert.That(session.Phase, Is.EqualTo(FishingPhase.Casting));
            Assert.That(Get<FishingVisuals>(session, "visuals").WaterTarget, Is.EqualTo(new Vector3(4f, 0f, 7f)));
            Assert.That(Get<bool>(tools, "mobileInteractPressed"), Is.False, "Cast must not queue a farm action.");
            tools.SetMobileInteractHeld(true);
            typeof(PlayerToolController).GetMethod("Update", Private).Invoke(tools, null);
            Assert.That(FishingSession.Active, Is.SameAs(session));
            Assert.That(query.Calls, Is.Zero, "ACTION must not select a different world target.");
            Assert.That(Get<bool>(tools, "mobileInteractPressed"), Is.False);
        }

        [Test]
        public void HiddenCastPreservesNormalActionInput()
        {
            hud.SetSpot(spot, false);
            tools.SetMobileInteractHeld(true);
            Assert.That(FishingSession.IsFishing, Is.False);
            Assert.That(Get<bool>(tools, "mobileInteractPressed"), Is.True);
            Assert.That(Get<bool>(tools, "mobileInteractHeld"), Is.True);
            tools.SetMobileInteractHeld(false);
            Assert.That(Get<bool>(tools, "mobileInteractReleased"), Is.True);
        }

        [Test]
        public void StaleVisibleCastRechecksRangeAndFallsBackWithoutStarting()
        {
            hud.SetSpot(spot, true);
            tools.transform.position = new Vector3(20f, 0f, 0f);
            tools.SetMobileInteractHeld(true);
            Assert.That(FishingSession.IsFishing, Is.False);
            Assert.That(Get<bool>(tools, "mobileInteractPressed"), Is.True);
            Assert.That(FishingHud.TryCastFromAction(), Is.False);
        }

        private sealed class CountingQuery : IInteractionTargetQuery
        {
            public int Calls;
            public bool TryFindTarget(Vector3 origin, float range, out IInteractable target)
            { Calls++; target = null; return false; }
        }
    }
}
