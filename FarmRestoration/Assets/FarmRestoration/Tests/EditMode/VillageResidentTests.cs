using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class VillageResidentTests
    {
        [Test]
        public void ResidentKeepsTwoIndependentSocialTopicsWithMultipleLines()
        {
            GameObject actor = new GameObject("TestResident");
            VillageResidentNpc resident = actor.AddComponent<VillageResidentNpc>();
            try
            {
                string[] first = { "Hello", "Nice weather" };
                resident.Configure("Maya", "Good day", "How are you?", first,
                    "About the village", new[] { "We have a well", "Everyone helps" });
                first[0] = "Changed outside";
                Assert.That(resident.ResidentName, Is.EqualTo("Maya"));
                Assert.That(resident.Greeting, Is.EqualTo("Good day"));
                Assert.That(resident.ReplyCount(0), Is.EqualTo(2));
                Assert.That(resident.ReplyCount(1), Is.EqualTo(2));
                Assert.That(resident.Reply(0, 0), Is.EqualTo("Hello"));
                Assert.That(resident.Reply(1, 1), Is.EqualTo("Everyone helps"));
            }
            finally { Object.DestroyImmediate(actor); }
        }

        [Test]
        public void InvalidTopicAndEmptyConfigurationDoNotThrow()
        {
            GameObject actor = new GameObject("EmptyResident");
            VillageResidentNpc resident = actor.AddComponent<VillageResidentNpc>();
            try
            {
                resident.Configure(" ", "", "First", null, "Second", null);
                Assert.That(resident.ResidentName, Is.EqualTo("Villager"));
                Assert.That(resident.Greeting, Is.EqualTo("Hello!"));
                Assert.That(resident.ReplyCount(9), Is.Zero);
                Assert.That(resident.Reply(0, -1), Is.Empty);
                Assert.That(resident.Reply(1, 0), Is.Empty);
            }
            finally { Object.DestroyImmediate(actor); }
        }
    }
}
