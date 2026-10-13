using TMPro;
using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class TreeHarvestVisual : MonoBehaviour
    {
        public const float FallDuration = 1.65f;
        private ChoppableTree tree;
        private Transform model;
        private Quaternion modelRotation;
        private GameObject stump;
        private BoxCollider trunk;
        private Material stumpMaterial;
        private TMP_Text marker;
        private PlayerToolController player;
        private Camera view;
        private float fallTime;
        private Vector3 fallAxis;
        private bool falling;

        public void Configure(ChoppableTree owner, Transform treeModel)
        {
            tree = owner;
            foreach (Collider collider in treeModel.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            Transform pivot = new GameObject("TreeFallPivot").transform;
            pivot.SetParent(transform, false);
            treeModel.SetParent(pivot, true);
            model = pivot;
            modelRotation = model.localRotation;
            trunk = GetComponent<BoxCollider>();
            if (trunk == null) trunk = gameObject.AddComponent<BoxCollider>();
            stump = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stump.name = "TreeStump";
            stump.transform.SetParent(transform, false);
            stump.transform.localPosition = Vector3.up * 0.18f;
            stump.transform.localScale = new Vector3(0.7f, 0.18f, 0.7f);
            Collider stumpCollider = stump.GetComponent<Collider>();
            stumpCollider.enabled = false;
            Renderer source = model.GetComponentInChildren<Renderer>(true);
            if (source != null && source.sharedMaterial != null)
            {
                stumpMaterial = new Material(source.sharedMaterial.shader);
                stumpMaterial.color = new Color(0.40f, 0.24f, 0.11f);
                stump.GetComponent<Renderer>().sharedMaterial = stumpMaterial;
            }
            if (Application.isPlaying)
            {
                GameObject label = new GameObject("WoodTreeMarker", typeof(TextMeshPro));
                label.transform.SetParent(transform, false);
                label.transform.localPosition = Vector3.up * 2.6f;
                marker = label.GetComponent<TextMeshPro>();
                marker.fontSize = 2.5f;
                marker.color = new Color(1f, 0.9f, 0.6f);
                marker.alignment = TextAlignmentOptions.Center;
                marker.rectTransform.sizeDelta = new Vector2(7f, 1.5f);
                player = FindAnyObjectByType<PlayerToolController>();
                view = Camera.main;
            }
            Refresh();
        }

        public void Refresh()
        {
            if (tree == null || model == null || stump == null) return;
            falling = false;
            model.localRotation = modelRotation;
            model.gameObject.SetActive(!tree.State.IsStump);
            stump.SetActive(tree.State.IsStump);
            trunk.center = Vector3.up * (tree.State.IsStump ? 0.2f : 1.2f);
            trunk.size = tree.State.IsStump ? new Vector3(0.7f, 0.4f, 0.7f) : new Vector3(0.8f, 2.4f, 0.8f);
            if (marker != null) marker.text = tree.State.IsStump ? "STUMP - GROWING"
                : "WOOD TREE\n" + tree.State.HitsRemaining + " hits [E / ACTION]";
        }

        public void Fall()
        {
            Refresh();
            model.gameObject.SetActive(true);
            Vector3 direction = player == null ? Vector3.forward : transform.position - player.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
            fallAxis = transform.InverseTransformDirection(Vector3.Cross(Vector3.up, direction.normalized));
            fallTime = 0f;
            falling = true;
        }

        private void Update()
        {
            if (!falling) return;
            fallTime += Time.deltaTime;
            float progress = Mathf.Clamp01(fallTime / FallDuration);
            model.localRotation = Quaternion.AngleAxis(85f * progress * progress, fallAxis) * modelRotation;
            if (progress >= 1f) Refresh();
        }

        private void LateUpdate()
        {
            if (marker == null) return;
            bool show = player != null && player.SelectedTool == FarmTool.Axe
                && (player.transform.position - transform.position).sqrMagnitude <= 18f * 18f;
            marker.gameObject.SetActive(show);
            if (show && view != null) marker.transform.rotation = view.transform.rotation;
        }

        private void OnDestroy()
        {
            if (stumpMaterial == null) return;
            if (Application.isPlaying) Destroy(stumpMaterial);
            else DestroyImmediate(stumpMaterial);
        }
    }
}
