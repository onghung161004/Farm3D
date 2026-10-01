using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class PlayerToolVisual : MonoBehaviour
    {
        [SerializeField] private PlayerToolController toolController;
        [SerializeField] private Transform toolHolder;
        [SerializeField, Min(0.05f)] private float useDuration = 0.28f;
        [SerializeField, Min(0.01f)] private float bobDistance = 0.28f;

        private Vector3 holderBasePosition;
        private Quaternion holderBaseRotation;
        private float useTimeRemaining;
        private bool isSubscribed;

        public bool IsConfigured => toolController != null && toolHolder != null;

        private void Awake()
        {
            if (toolController == null) toolController = GetComponent<PlayerToolController>();
            TryInitialize();
        }

        private void OnEnable()
        {
            if (TryInitialize()) Subscribe();
        }

        private void OnDisable()
        {
            if (!isSubscribed || toolController == null) return;
            toolController.SelectedToolChanged -= HandleSelectedToolChanged;
            toolController.ToolUsedSuccessfully -= HandleSuccessfulToolUse;
            isSubscribed = false;
        }

        private void Update()
        {
            if (useTimeRemaining <= 0f) return;
            useTimeRemaining = Mathf.Max(0f, useTimeRemaining - Time.deltaTime);
            float progress = 1f - (useTimeRemaining / useDuration);
            toolHolder.localPosition = holderBasePosition + PlayerToolVisualState.GetUseBobOffset(progress, bobDistance);
            toolHolder.localRotation = holderBaseRotation * Quaternion.Euler(45f * Mathf.Sin(progress * Mathf.PI), 0f, 0f);
            if (useTimeRemaining <= 0f)
            {
                toolHolder.localPosition = holderBasePosition;
                toolHolder.localRotation = holderBaseRotation;
            }
        }

        public void Configure(PlayerToolController controller, Transform holder)
        {
            toolController = controller;
            toolHolder = holder;
            enabled = true;
            if (TryInitialize()) Subscribe();
        }

        private void HandleSelectedToolChanged(FarmTool tool) => ShowTool(tool);
        private void HandleSuccessfulToolUse(FarmTool tool) => useTimeRemaining = useDuration;

        private void ShowTool(FarmTool tool)
        {
            if (toolHolder == null) return;
            string rootName = PlayerToolVisualState.GetVisualRootName(tool);
            for (int index = 0; index < toolHolder.childCount; index++)
            {
                Transform child = toolHolder.GetChild(index);
                child.gameObject.SetActive(child.name == rootName);
            }
        }

        private bool TryInitialize()
        {
            if (!IsConfigured) return false;
            holderBasePosition = toolHolder.localPosition;
            holderBaseRotation = toolHolder.localRotation;
            ShowTool(toolController.SelectedTool);
            return true;
        }

        private void Subscribe()
        {
            if (isSubscribed) return;
            toolController.SelectedToolChanged += HandleSelectedToolChanged;
            toolController.ToolUsedSuccessfully += HandleSuccessfulToolUse;
            isSubscribed = true;
        }
    }
}
