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
        [SerializeField] private LayerMask interactionMask = Physics.DefaultRaycastLayers;

        private readonly RaycastHit[] raycastHits = new RaycastHit[8];
        private readonly Collider[] nearbyColliders = new Collider[8];
        private readonly List<MonoBehaviour> componentBuffer = new List<MonoBehaviour>(8);
        private IInteractionTargetQuery targetQuery;
        private IInteractable currentTarget;
        private string currentPrompt;

        public FarmTool SelectedTool { get; private set; } = FarmTool.Hoe;
        public string CurrentPrompt => currentPrompt;

        public event Action<FarmTool> SelectedToolChanged;
        public event Action<FarmTool> ToolUsedSuccessfully;
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
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                TryUseTool();
            }

            if (keyboard != null && keyboard.gKey.wasPressedThisFrame)
            {
                TryAdvanceDemoGrowth();
            }
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
                ToolUsedSuccessfully?.Invoke(SelectedTool);
            }
            UpdateInteractionPrompt();
            return didInteract;
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
