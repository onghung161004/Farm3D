using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class VillageTraderRoutineTests
    {
        [Test]
        public void TraderTreatsOnlyLateNightAndEarlyMorningAsNight()
        {
            Assert.That(VillageTraderRoutine.IsNight(0.10f), Is.True);
            Assert.That(VillageTraderRoutine.IsNight(0.50f), Is.False);
            Assert.That(VillageTraderRoutine.IsNight(0.90f), Is.True);
        }

        [Test]
        public void DialoguePauseCanBeReleased()
        {
            GameObject actor = new GameObject("TraderRoutineTest");
            VillageTraderRoutine routine = actor.AddComponent<VillageTraderRoutine>();
            try
            {
                routine.SetPaused(true);
                Assert.That(routine.IsPaused, Is.True);
                routine.SetPaused(false);
                Assert.That(routine.IsPaused, Is.False);
            }
            finally { Object.DestroyImmediate(actor); }
        }
    }
}
