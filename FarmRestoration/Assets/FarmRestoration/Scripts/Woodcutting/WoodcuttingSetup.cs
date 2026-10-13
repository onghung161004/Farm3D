using System.Collections.Generic;
using UnityEngine;

namespace FarmRestoration
{
    public static class WoodcuttingSetup
    {
        public static void EnsureInFarm()
        {
            Transform valley = GameObject.Find("IllustratedValleyMap")?.transform;
            Transform forest = valley?.Find("PolytopeNatureEnvironment/Trees");
            if (forest == null) return;
            EnsureTrees(forest, valley);
            CowPen pen = Object.FindAnyObjectByType<CowPen>();
            if (pen != null) LumberWorkshop.Ensure(pen);
        }

        public static void EnsureTrees(Transform forest, Transform parent)
        {
            if (forest == null || parent == null) return;
            Transform root = parent.Find("WoodcuttingTrees");
            if (root != null && root.GetComponentInChildren<ChoppableTree>(true) != null) return;
            if (root == null)
            {
                root = new GameObject("WoodcuttingTrees").transform;
                root.SetParent(parent, false);
            }
            var candidates = new List<Transform>();
            foreach (Transform model in forest)
                if (model.gameObject.activeInHierarchy && model.name.Contains("Pine")) candidates.Add(model);
            candidates.Sort((a, b) =>
            {
                int order = a.position.sqrMagnitude.CompareTo(b.position.sqrMagnitude);
                return order != 0 ? order : string.CompareOrdinal(a.name, b.name);
            });
            for (int i = 0; i < candidates.Count && i < 12; i++)
            {
                Transform model = candidates[i];
                Vector3 position = model.position;
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue;
                float bottom = float.MaxValue;
                foreach (Renderer renderer in renderers) bottom = Mathf.Min(bottom, renderer.bounds.min.y);
                position.y = bottom;
                Transform wrapper = new GameObject("WoodTree_" + model.name).transform;
                wrapper.SetParent(root, false);
                wrapper.position = position;
                string id = "wood-tree/" + model.name;
                model.SetParent(wrapper, true);
                wrapper.gameObject.AddComponent<ChoppableTree>().Configure(id, model);
            }
        }
    }
}
