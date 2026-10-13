using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class CowBreedingTests
    {
        private GameObject root;
        private CowPen pen;
        private List<CowAnimal> herd;
        private DateTime now;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("BreedingTest");
            var penObject = new GameObject("DairyPen");
            penObject.transform.SetParent(root.transform);
            pen = penObject.AddComponent<CowPen>();
            pen.Configure(new Vector2(8f, 18f));
            herd = new List<CowAnimal>();
            now = DateTime.UtcNow;
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(root);

        private CowAnimal Adult(string id, float x = 0f, float z = 0f, CowPen home = null)
        {
            var cowObject = new GameObject(id);
            cowObject.transform.SetParent(root.transform);
            CowAnimal cow = cowObject.AddComponent<CowAnimal>();
            cow.Configure(id, home == null ? pen : home, null, null, null);
            cow.Restore(new CowSaveData { id = id, penned = true, x = x, z = z });
            herd.Add(cow);
            return cow;
        }

        private CowAnimal Breed(CowAnimal cow, DateTime time, out string message)
        {
            CowAnimal calf = CowBreeding.TryBreed(cow, herd, time, out message);
            if (calf != null) herd.Add(calf);
            return calf;
        }

        [Test]
        public void TwoFedAdultsProduceOneCalfAndStillGiveMilk()
        {
            CowAnimal a = Adult("parent-a", -1f, 4f);
            CowAnimal b = Adult("parent-b", 1f, 4f);
            Assert.That(a.Feed(now), Is.True);
            Assert.That(Breed(a, now, out _), Is.Null);
            Assert.That(b.Feed(now), Is.True);
            CowAnimal calf = Breed(b, now, out string message);

            Assert.That(calf, Is.Not.Null);
            Assert.That(calf.IsPenned, Is.True);
            Assert.That(calf.IsCalf, Is.True);
            Assert.That(pen.Contains(calf.transform.position, 1.2f), Is.True);
            Assert.That(calf.transform.localScale.x, Is.EqualTo(CowLifeCycle.CalfScale).Within(0.002f));
            Assert.That(calf.Snapshot().milkReadyUtcTicks, Is.Zero, "A cloned calf must not inherit its parent's milk batch.");
            Assert.That(message, Does.Contain("calf was born"));
            Assert.That(a.CanMilk(now.AddSeconds(120)), Is.True);
            Assert.That(b.CanMilk(now.AddSeconds(120)), Is.True);
            Assert.That(Breed(a, now, out _), Is.Null, "The same meals cannot produce a second calf.");
            Assert.That(herd.Count, Is.EqualTo(3));
        }

        [Test]
        public void ExpiredFirstMealCannotBePairedWithLaterMeal()
        {
            CowAnimal a = Adult("a");
            CowAnimal b = Adult("b", 1f, 4f);
            a.Feed(now);
            DateTime later = now.AddSeconds(CowLifeCycle.FeedingWindowSeconds);
            b.Feed(later);
            Assert.That(Breed(b, later, out _), Is.Null);
            Assert.That(a.LifeCycle.CanBreed(later), Is.False);
        }

        [Test]
        public void FedCowsInDifferentPensDoNotBreed()
        {
            var otherObject = new GameObject("OtherPen");
            otherObject.transform.SetParent(root.transform);
            CowPen otherPen = otherObject.AddComponent<CowPen>();
            CowAnimal a = Adult("a");
            CowAnimal b = Adult("b", 1f, 4f, otherPen);
            a.Feed(now);
            b.Feed(now);
            Assert.That(Breed(b, now, out _), Is.Null);
        }

        [Test]
        public void FullPenStillFeedsForMilkButDoesNotSpawnCalf()
        {
            for (int i = 0; i < pen.Capacity; i++) Adult("cow-" + i, i % 2 == 0 ? -1f : 1f, i);
            herd[0].Feed(now);
            herd[1].Feed(now);
            Assert.That(Breed(herd[1], now, out string message), Is.Null);
            Assert.That(message, Does.Contain("Pen full"));
            Assert.That(herd[0].CanMilk(now.AddSeconds(120)), Is.True);
            Assert.That(herd.Count, Is.EqualTo(pen.Capacity));
        }

        [Test]
        public void CooldownRequiresNewMealsAndEndsExactlyAtDeadline()
        {
            CowLifeCycle life = Adult("a").LifeCycle;
            life.RecordFeed(now);
            life.CompleteBreeding(now);
            life.RecordFeed(now.AddSeconds(299));
            Assert.That(life.CanBreed(now.AddSeconds(299)), Is.False);
            Assert.That(life.CanBreed(now.AddSeconds(300)), Is.False, "An old meal must not reactivate after the cooldown.");
            life.RecordFeed(now.AddSeconds(300));
            Assert.That(life.CanBreed(now.AddSeconds(300)), Is.True);
        }

        [Test]
        public void CalfCannotFeedMilkOrBreedAndGrowthUsesSavedUtcTime()
        {
            CowAnimal calf = Adult("calf");
            var saved = new CowSaveData
            {
                id = "calf", penned = true, offspring = true,
                bornUtcTicks = now.Ticks, grownUtcTicks = now.AddSeconds(300).Ticks
            };
            calf.Restore(saved);
            Assert.That(calf.Feed(now), Is.False);
            Assert.That(calf.CanMilk(now), Is.False);
            Assert.That(calf.LifeCycle.CanBreed(now), Is.False);
            calf.LifeCycle.Tick(now.AddSeconds(150));
            Assert.That(calf.transform.localScale.x, Is.EqualTo(0.725f).Within(0.001f));
            calf.LifeCycle.Tick(now.AddSeconds(300));
            Assert.That(calf.LifeCycle.IsCalfAt(now.AddSeconds(300)), Is.False);
            Assert.That(calf.transform.localScale, Is.EqualTo(Vector3.one));

            saved.bornUtcTicks = now.AddSeconds(-600).Ticks;
            saved.grownUtcTicks = now.AddSeconds(-300).Ticks;
            calf.Restore(saved);
            Assert.That(calf.IsCalf, Is.False, "Offline time counts towards growth.");
            Assert.That(calf.CanFeed, Is.True);
        }

        [Test]
        public void SavedOffspringIsRestoredOnceWithAgeAndUniqueUnicodeId()
        {
            Adult("adult-template");
            var saved = new CowSaveData
            {
                id = "bê-con-α", offspring = true, penned = true, penId = pen.PenId,
                x = 1f, z = 3f, bornUtcTicks = now.Ticks, grownUtcTicks = now.AddSeconds(300).Ticks
            };
            FarmSaveData data = JsonUtility.FromJson<FarmSaveData>(JsonUtility.ToJson(new FarmSaveData
            {
                milk = 2, fish = 3, coins = 61, cows = new[] { saved }
            }));
            CowBreeding.RestoreOffspring(data.cows, herd);
            CowBreeding.RestoreOffspring(data.cows, herd);
            Assert.That(herd.Count, Is.EqualTo(2));
            Assert.That(herd[1].CowId, Is.EqualTo(saved.id));
            Assert.That(herd[1].Snapshot().grownUtcTicks, Is.EqualTo(saved.grownUtcTicks));
            Assert.That(herd[1].IsCalf, Is.True);
            Assert.That(data.coins, Is.EqualTo(61));
            Assert.That(data.fish, Is.EqualTo(3));
            Assert.That(data.milk, Is.EqualTo(2));
        }

        [Test]
        public void VersionFourCowRemainsAdultAndKeepsMilkDeadline()
        {
            FarmSaveData data = JsonUtility.FromJson<FarmSaveData>(
                "{\"version\":4,\"coins\":42,\"cows\":[{\"id\":\"cow-1\",\"penned\":true,\"milkReadyUtcTicks\":1}]}");
            CowAnimal cow = Adult("cow-1");
            cow.Restore(data.cows[0]);
            Assert.That(cow.IsCalf, Is.False);
            Assert.That(cow.CanMilk(now), Is.True);
            Assert.That(cow.LifeCycle.CanBreed(now), Is.False);
            Assert.That(cow.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(data.coins, Is.EqualTo(42));
        }

        [Test]
        public void NullOrEmptyHerdAndMissingSnapshotsAreSafe()
        {
            Assert.That(CowBreeding.TryBreed(null, herd, now, out _), Is.Null);
            Assert.That(CowBreeding.TryBreed(Adult("a"), null, now, out _), Is.Null);
            Assert.DoesNotThrow(() => CowBreeding.RestoreOffspring(null, herd));
            Assert.DoesNotThrow(() => CowBreeding.RestoreOffspring(new[] { (CowSaveData)null }, herd));
            Assert.That(CowOffspringFactory.Create(null, null), Is.Null);
        }

        [Test]
        public void GameplayFeedSpendsTwoCarrotsRegistersCalfAndIncludesItInSave()
        {
            CowAnimal a = Adult("a", -1f, 4f);
            CowAnimal b = Adult("b", 1f, 4f);
            GameObject hudObject = new GameObject("BreedingInventory");
            hudObject.transform.SetParent(root.transform);
            hudObject.SetActive(false);
            FarmHud hud = hudObject.AddComponent<FarmHud>();
            FarmProgression game = hudObject.AddComponent<FarmProgression>();
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(FarmProgression).GetField("hud", fields).SetValue(game, hud);
            typeof(FarmProgression).GetField("coins", fields).SetValue(game, 61);
            var registered = (List<CowAnimal>)game.Cows;
            registered.AddRange(herd);
            hud.CropInventory.Add(CropType.Carrot, 2);
            // Capture the transaction without writing to any player save file.
            game.Changed += () => typeof(FarmProgression).GetField("ready", fields).SetValue(game, false);
            typeof(FarmProgression).GetField("ready", fields).SetValue(game, true);
            Assert.That(game.TryFeedCow(a), Is.True);
            typeof(FarmProgression).GetField("ready", fields).SetValue(game, true);
            Assert.That(game.TryFeedCow(b), Is.True);
            Assert.That(hud.CropInventory.GetCount(CropType.Carrot), Is.Zero);
            Assert.That(game.Cows.Count, Is.EqualTo(3));
            FarmSaveData saved = JsonUtility.FromJson<FarmSaveData>(JsonUtility.ToJson(game.Snapshot()));
            Assert.That(saved.version, Is.EqualTo(FarmSaveData.CurrentVersion));
            Assert.That(saved.coins, Is.EqualTo(61));
            Assert.That(saved.cows.Length, Is.EqualTo(3));
            Assert.That(saved.cows[2].offspring, Is.True);
            Assert.That(saved.cows[2].id, Is.Not.EqualTo(saved.cows[0].id));
            Assert.That(saved.cows[0].fedUntilUtcTicks, Is.Zero);
            Assert.That(saved.cows[1].fedUntilUtcTicks, Is.Zero);
        }

        [Test]
        public void OffspringReusesModelAndMaterialAtSmallerScale()
        {
            CowAnimal template = Adult("template");
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "CowModel";
            visual.transform.SetParent(template.transform, false);
            Material material = visual.GetComponent<Renderer>().sharedMaterial;
            var saved = new CowSaveData
            {
                id = "calf-model", offspring = true, penned = true, penId = pen.PenId,
                x = 1f, z = 4f, bornUtcTicks = now.Ticks, grownUtcTicks = now.AddSeconds(300).Ticks
            };
            CowAnimal calf = CowOffspringFactory.Create(template, saved);
            Assert.That(calf.transform.Find("CowModel"), Is.Not.Null);
            Assert.That(calf.GetComponentInChildren<Renderer>().sharedMaterial, Is.SameAs(material));
            Assert.That(calf.transform.localScale.x, Is.LessThan(template.transform.localScale.x));
        }

        [Test]
        public void ObstructedPenDoesNotSpawnInsidePropsOrConsumeParentBreedingState()
        {
            CowAnimal a = Adult("a", -1f, 4f);
            CowAnimal b = Adult("b", 1f, 4f);
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.SetParent(root.transform);
            obstacle.transform.position = new Vector3(0f, 1f, 0f);
            obstacle.transform.localScale = new Vector3(20f, 2f, 30f);
            Physics.SyncTransforms();
            a.Feed(now);
            b.Feed(now);
            Assert.That(Breed(b, now, out string message), Is.Null);
            Assert.That(message, Does.Contain("No clear space"));
            Assert.That(a.LifeCycle.CanBreed(now), Is.True);
            Assert.That(b.LifeCycle.CanBreed(now), Is.True);
        }
    }
}
