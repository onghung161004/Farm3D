using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class FarmGardenBatchTests
    {
        private readonly List<GameObject> roots = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject root in roots) Object.DestroyImmediate(root);
            roots.Clear();
        }

        [Test]
        public void FindNearest_ChoosesOnlyReachableGarden()
        {
            Transform pumpkin = CreateGarden("PumpkinGarden", new Vector3(1000f, 0f, 1000f));
            CreateGarden("CarrotGarden", new Vector3(1010f, 0f, 1000f));

            Transform found = FarmGardenBatch.FindNearest(new Vector3(1000f, 0f, 999f), 3f);
            Transform tooFar = FarmGardenBatch.FindNearest(new Vector3(1000f, 0f, 990f), 3f);

            Assert.That(found, Is.SameAs(pumpkin));
            Assert.That(tooFar, Is.Null);
        }

        [Test]
        public void GetOrderedPlots_ReturnsNineUniquePlotsFromOneGarden()
        {
            Transform pumpkin = CreateGarden("PumpkinGarden", new Vector3(1000f, 0f, 1000f));
            Transform tomato = CreateGarden("TomatoGarden", new Vector3(1010f, 0f, 1000f));

            FarmPlot[] plots = FarmGardenBatch.GetOrderedPlots(pumpkin, new Vector3(1000f, 0f, 998f));

            Assert.That(plots, Has.Length.EqualTo(9));
            Assert.That(new HashSet<FarmPlot>(plots).Count, Is.EqualTo(9));
            Assert.That(plots[0].transform.parent, Is.SameAs(pumpkin));
            foreach (FarmPlot plot in plots) Assert.That(plot.transform.parent, Is.Not.SameAs(tomato));
        }

        [Test]
        public void IncompleteGarden_IsNotEligibleForBatch()
        {
            Transform garden = CreateGarden("CarrotGarden", new Vector3(1000f, 0f, 1000f), 8);

            Assert.That(FarmGardenBatch.FindNearest(new Vector3(1000f, 0f, 1000f), 3f), Is.Null);
            Assert.That(FarmGardenBatch.GetOrderedPlots(garden, Vector3.zero), Is.Empty);
        }

        private Transform CreateGarden(string name, Vector3 center, int count = 9)
        {
            GameObject root = new GameObject(name);
            root.transform.position = center;
            roots.Add(root);
            for (int i = 0; i < count; i++)
            {
                GameObject child = new GameObject("Plot_" + i);
                child.transform.SetParent(root.transform, false);
                child.transform.localPosition = new Vector3((i % 3 - 1) * 1.25f, 0f, (i / 3 - 1) * 1.25f);
                child.AddComponent<FarmPlot>();
            }
            return root.transform;
        }
    }
}
