using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class PlayerToolController : MonoBehaviour
    {
        private const string NoTargetPrompt = "Move closer to a farm plot";

        [SerializeField] private Camera interactionCamera;
        [SerializeField, Min(0.1f)] private float interactionRange = 3f;
        [SerializeField, Min(0.05f)] private float batchHoldSeconds = 0.25f;
        [SerializeField, Min(0.05f)] private float batchPlotInterval = 0.18f;
        [SerializeField] private LayerMask interactionMask = Physics.DefaultRaycastLayers;

        private readonly RaycastHit[] raycastHits = new RaycastHit[8];
        private readonly Collider[] nearbyColliders = new Collider[8];
        private readonly List<MonoBehaviour> componentBuffer = new List<MonoBehaviour>(8);
        private IInteractionTargetQuery targetQuery;
        private IInteractable currentTarget;
        private string currentPrompt;
        private Transform heldGarden;
        private FarmPlot heldTapPlot;
        private FarmPlot[] batchPlots;
        private FarmTool heldTool;
        private float heldSince;
        private float nextBatchTime;
        private int batchIndex;
        private bool batchStarted;

        public FarmTool SelectedTool { get; private set; } = FarmTool.Hoe;
        public string CurrentPrompt => currentPrompt;

        public event Action<FarmTool> SelectedToolChanged;
        public event Action<FarmTool> ToolUsedSuccessfully;
        public event Action<FarmTool, IInteractable> InteractionSucceeded;
        public event Action<string> InteractionPromptChanged;

        private void Awake()
        {
            if (interactionCamera == null)
            {
                interactionCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            PublishSelection();
            UpdateInteractionPrompt();
        }

        private void Update()
        {
            HandleToolSelectionInput();
            UpdateInteractionPrompt();

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null) HandleInteractKey(keyboard);

            if (keyboard != null && keyboard.rKey.wasPressedThisFrame
                && TryFindTarget(out IInteractable recipeTarget)
                && recipeTarget is FarmProductionStation station && station.CycleRecipe())
            {
                UpdateInteractionPrompt();
            }

        }

        private void OnDisable() => CancelHeldGarden();

        private void HandleInteractKey(Keyboard keyboard)
        {
            if (keyboard.eKey.wasPressedThisFrame)
            {
                IInteractable target = TryFindTarget(out IInteractable found) ? found : null;
                if (target != null && !(target is FarmPlot))
                {
                    TryUseTool();
                    return;
                }

                Transform garden = FarmGardenBatch.FindNearest(transform.position, interactionRange);
                if (garden == null)
                {
                    TryUseTool();
                    return;
                }

                heldGarden = garden;
                heldTapPlot = target as FarmPlot;
                if (heldTapPlot == null || !heldTapPlot.transform.IsChildOf(garden))
                    heldTapPlot = null;
                heldTool = SelectedTool;
                heldSince = Time.unscaledTime;
                batchPlots = FarmGardenBatch.GetOrderedPlots(garden, transform.position);
                batchIndex = 0;
                batchStarted = false;
            }

            if (heldGarden == null) return;
            if (SelectedTool != heldTool
                || !FarmGardenBatch.IsWithinRange(heldGarden, transform.position, interactionRange))
            {
                CancelHeldGarden();
                return;
            }

            if (keyboard.eKey.wasReleasedThisFrame)
            {
                if (!batchStarted)
                {
                    FarmPlot tapPlot = heldTapPlot != null ? heldTapPlot
                        : batchPlots != null && batchPlots.Length > 0 ? batchPlots[0] : null;
                    UsePlot(tapPlot);
                }
                CancelHeldGarden();
                return;
            }

            if (!keyboard.eKey.isPressed || Time.unscaledTime - heldSince < batchHoldSeconds) return;
            if (!batchStarted)
            {
                batchStarted = true;
                nextBatchTime = Time.unscaledTime;
            }
            if (batchPlots == null || batchIndex >= batchPlots.Length || Time.unscaledTime < nextBatchTime) return;

            UsePlot(batchPlots[batchIndex++]);
            nextBatchTime = Time.unscaledTime + batchPlotInterval;
        }

        private void UsePlot(FarmPlot plot)
        {
            if (plot == null || !plot.isActiveAndEnabled) return;
            if (PlayerToolInteraction.TryUse(plot, heldTool)) PublishSuccessfulInteraction(heldTool, plot);
            UpdateInteractionPrompt();
        }

        private void CancelHeldGarden()
        {
            heldGarden = null;
            heldTapPlot = null;
            batchPlots = null;
            batchIndex = 0;
            batchStarted = false;
        }

        public void ConfigureInteractionCamera(Camera cameraToUse)
        {
            interactionCamera = cameraToUse;
        }

        public void ConfigureTargetQuery(IInteractionTargetQuery query)
        {
            targetQuery = query;
        }

        public void SelectTool(int shortcutIndex)
        {
            if (PlayerToolSelection.TryGetTool(shortcutIndex, out FarmTool nextTool))
            {
                SetSelectedTool(nextTool);
            }
        }

        public bool TryUseTool()
        {
            if (!TryFindTarget(out IInteractable target))
            {
                return false;
            }

            bool didInteract = PlayerToolInteraction.TryUse(target, SelectedTool);
            if (didInteract)
            {
                PublishSuccessfulInteraction(SelectedTool, target);
            }
            UpdateInteractionPrompt();
            return didInteract;
        }

        private void PublishSuccessfulInteraction(FarmTool tool, IInteractable target)
        {
            ToolUsedSuccessfully?.Invoke(tool);
            InteractionSucceeded?.Invoke(tool, target);
        }

        public bool TryAdvanceDemoGrowth()
        {
            if (!TryFindTarget(out IInteractable target))
            {
                return false;
            }

            bool advanced = FarmGrowthDemo.TryAdvance(target);
            UpdateInteractionPrompt();
            return advanced;
        }

        private void HandleToolSelectionInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                SelectTool(1);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                SelectTool(2);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                SelectTool(3);
            }
            else if (keyboard.digit4Key.wasPressedThisFrame)
            {
                SelectTool(4);
            }
        }

        private void SetSelectedTool(FarmTool nextTool)
        {
            if (SelectedTool == nextTool)
            {
                return;
            }

            CancelHeldGarden();

            SelectedTool = nextTool;
            PublishSelection();
            UpdateInteractionPrompt();
        }

        private void PublishSelection()
        {
            SelectedToolChanged?.Invoke(SelectedTool);
        }

        private void UpdateInteractionPrompt()
        {
            IInteractable target = TryFindTarget(out IInteractable foundTarget) ? foundTarget : null;
            string nextPrompt = target == null ? NoTargetPrompt : target.GetInteractionPrompt(SelectedTool);
            if (target is FarmPlot plot && FarmGardenBatch.IsGardenPlot(plot))
                nextPrompt += "  |  Hold E: all 9 plots";
            if (ReferenceEquals(currentTarget, target) && currentPrompt == nextPrompt)
            {
                return;
            }

            currentTarget = target;
            currentPrompt = nextPrompt;
            InteractionPromptChanged?.Invoke(nextPrompt);
        }

        private bool TryFindTarget(out IInteractable target)
        {
            if (targetQuery != null)
            {
                return targetQuery.TryFindTarget(transform.position, interactionRange, out target);
            }

            IInteractable closestTarget = FindClosestForwardTarget();
            if (closestTarget == null)
            {
                closestTarget = FindClosestNearbyTarget();
            }

            target = closestTarget;
            return target != null;
        }

        private IInteractable FindClosestForwardTarget()
        {
            Vector3 origin = transform.position + (Vector3.up * 0.75f);
            Vector3 direction = transform.forward;
            if (interactionCamera != null)
            {
                direction = Vector3.ProjectOnPlane(interactionCamera.transform.forward, Vector3.up).normalized;
                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = transform.forward;
                }
            }

            int hitCount = Physics.RaycastNonAlloc(
                origin,
                direction,
                raycastHits,
                interactionRange,
                interactionMask,
                QueryTriggerInteraction.Collide);
            IInteractable closestTarget = null;
            float closestDistance = float.MaxValue;
            for (int index = 0; index < hitCount; index++)
            {
                if (TryResolveInteractable(raycastHits[index].collider, out IInteractable target)
                    && raycastHits[index].distance < closestDistance)
                {
                    closestTarget = target;
                    closestDistance = raycastHits[index].distance;
                }
            }

            return closestTarget;
        }

        private IInteractable FindClosestNearbyTarget()
        {
            int colliderCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                interactionRange,
                nearbyColliders,
                interactionMask,
                QueryTriggerInteraction.Collide);
            IInteractable closestTarget = null;
            float closestDistanceSquared = float.MaxValue;
            for (int index = 0; index < colliderCount; index++)
            {
                if (!TryResolveInteractable(nearbyColliders[index], out IInteractable target))
                {
                    continue;
                }

                MonoBehaviour targetBehaviour = target as MonoBehaviour;
                if (targetBehaviour == null)
                {
                    continue;
                }

                float distanceSquared = (targetBehaviour.transform.position - transform.position).sqrMagnitude;
                if (distanceSquared < closestDistanceSquared)
                {
                    closestTarget = target;
                    closestDistanceSquared = distanceSquared;
                }
            }

            return closestTarget;
        }

        private bool TryResolveInteractable(Collider collider, out IInteractable target)
        {
            Transform current = collider.transform;
            while (current != null)
            {
                componentBuffer.Clear();
                current.GetComponents(componentBuffer);
                if (InteractionTargetResolver.TryGetInteractable(componentBuffer, out target))
                {
                    return true;
                }

                current = current.parent;
            }

            target = null;
            return false;
        }
    }
}
