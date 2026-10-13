using System;
using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class ChoppableTree : MonoBehaviour, IInteractable
    {
        public const float HitInterval = 0.55f;
        private string treeId;
        private readonly TreeHarvestState state = new TreeHarvestState();
        private float lastHitTime = float.NegativeInfinity;
        private TreeHarvestVisual visual;
        public string TreeId => treeId;
        public TreeHarvestState State => state;
        public event Action<ChoppableTree> StateChanged;

        public void Configure(string id, Transform model)
        {
            treeId = id;
            if (model == null) return;
            visual = GetComponent<TreeHarvestVisual>() ?? gameObject.AddComponent<TreeHarvestVisual>();
            visual.Configure(this, model);
        }

        private void Update()
        {
            if (!state.Tick(DateTime.UtcNow)) return;
            if (visual != null) visual.Refresh();
            StateChanged?.Invoke(this);
        }

        public bool TryInteract(FarmTool tool) => tool == FarmTool.Axe
            && FarmProgression.Instance != null && FarmProgression.Instance.TryChopTree(this);

        public bool TryChop(DateTime now, float inputTime, out bool felled)
        {
            felled = false;
            if (inputTime - lastHitTime < HitInterval || !state.TryHit(now, out felled)) return false;
            lastHitTime = inputTime;
            if (felled && Application.isPlaying) TreeFallAudio.PlayFor(this);
            if (visual != null)
            {
                if (felled) visual.Fall();
                else visual.Refresh();
            }
            StateChanged?.Invoke(this);
            return true;
        }

        public string GetInteractionPrompt(FarmTool tool)
        {
            if (state.IsStump)
            {
                int seconds = Math.Max(0, (int)Math.Ceiling((state.RegrowUtcTicks - DateTime.UtcNow.Ticks) / (double)TimeSpan.TicksPerSecond));
                return "Tree stump: regrows in " + seconds + "s";
            }
            return tool == FarmTool.Axe ? "Chop tree: " + state.HitsRemaining + " hits left [E]"
                : "Wood tree: select Axe (8) to chop";
        }

        public TreeSaveData Snapshot() => state.Snapshot(treeId);
        public void Restore(TreeSaveData saved)
        {
            state.Restore(saved, DateTime.UtcNow);
            lastHitTime = float.NegativeInfinity;
            if (visual != null) visual.Refresh();
        }
    }
}
