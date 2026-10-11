using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmRestoration
{
    public enum FishingPhase { Idle, Casting, Waiting, Bite, Reeling, Caught, Lost }

    [DisallowMultipleComponent]
    public sealed class FishingSession : MonoBehaviour
    {
        public static FishingSession Active { get; private set; }
        public static bool IsFishing => Active != null;
        public FishingPhase Phase { get; private set; } = FishingPhase.Idle;
        public FishingPullState Pull { get; private set; }
        public float SafeCenter { get; private set; } = 0.5f;
        public string ResultMessage { get; private set; }

        private PlayerMovement player;
        private FishingHud hud;
        private FishingVisuals visuals;
        private FishingFeedback feedback;
        private float phaseTime;
        private float biteDelay;
        private float reelTime;
        private bool touchHeld;
        private bool pullHeld;
        private bool lastPullInput;

        public bool TryBegin(PlayerMovement movement, Vector3 waterTarget)
        {
            if (Active != null || movement == null || FarmProgression.Instance == null)
                return false;
            hud = FishingHud.Ensure();
            if (hud == null) { Debug.LogWarning("Fishing requires a HUD canvas.", this); return false; }
            player = movement;
            feedback = GetComponent<FishingFeedback>();
            if (feedback == null) feedback = gameObject.AddComponent<FishingFeedback>();
            visuals = FishingVisuals.Create(player.transform, waterTarget);
            Pull = FishingPullState.Initial;
            ResultMessage = null;
            SafeCenter = 0.5f;
            phaseTime = 0f;
            reelTime = 0f;
            touchHeld = false;
            pullHeld = false;
            lastPullInput = false;
            biteDelay = Random.Range(2.5f, 6f);
            Phase = FishingPhase.Casting;
            Active = this;
            movement.SetControlsLocked(true);
            Vector3 towardWater = waterTarget - player.transform.position;
            towardWater.y = 0f;
            if (towardWater.sqrMagnitude > 0.001f) player.transform.rotation = Quaternion.LookRotation(towardWater);
            hud.Show(this);
            return true;
        }

        private void Update()
        {
            if (Active != this) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) { Cancel(); return; }
            ApplyPullInput(touchHeld || (keyboard != null && keyboard.spaceKey.isPressed));

            float dt = Time.deltaTime;
            phaseTime += dt;
            switch (Phase)
            {
                case FishingPhase.Casting:
                    if (phaseTime >= 0.8f) { ChangePhase(FishingPhase.Waiting); feedback.PlayCast(); }
                    break;
                case FishingPhase.Waiting:
                    if (phaseTime >= biteDelay)
                    {
                        ChangePhase(FishingPhase.Bite);
                        feedback.PlayBite();
                    }
                    break;
                case FishingPhase.Bite:
                    if (phaseTime >= 3f) Finish(false, "Missed the bite. No fish added to warehouse.");
                    break;
                case FishingPhase.Reeling:
                    reelTime += dt;
                    SafeCenter = 0.5f + 0.14f * Mathf.Sin(reelTime * 1.35f);
                    Pull = FishingPullModel.Step(Pull, pullHeld, dt, SafeCenter);
                    if (Pull.caught) Finish(true);
                    else if (Pull.lost) Finish(false, "Fish escaped. No fish added to warehouse.");
                    break;
                case FishingPhase.Caught:
                case FishingPhase.Lost:
                    if (phaseTime >= 2f) Cancel();
                    break;
            }
            if (visuals != null) visuals.Refresh(Phase, phaseTime, Pull.progress);
            if (hud != null) hud.Refresh(this);
        }

        public void SetPullHeld(bool held)
        {
            if (Active != this) return;
            touchHeld = held;
            ApplyPullInput(held || (Keyboard.current != null && Keyboard.current.spaceKey.isPressed));
        }

        private void ApplyPullInput(bool held)
        {
            bool pressedNow = held && !lastPullInput;
            lastPullInput = held;
            pullHeld = held;
            if (pressedNow && Phase == FishingPhase.Bite)
            {
                ChangePhase(FishingPhase.Reeling);
                feedback.PlayHook();
            }
        }

        private void ChangePhase(FishingPhase next)
        {
            Phase = next;
            phaseTime = 0f;
        }

        private void Finish(bool caught, string failureMessage = null)
        {
            pullHeld = false;
            FarmProgression progression = FarmProgression.Instance;
            bool awarded = caught && progression != null && progression.AddFish();
            ResultMessage = awarded
                ? "+1 Fish  |  Warehouse: " + progression.FishCount + " fish"
                : caught ? "Could not add fish to warehouse. Check save/progression."
                : failureMessage ?? "No fish added to warehouse.";
            ChangePhase(awarded ? FishingPhase.Caught : FishingPhase.Lost);
            if (awarded) feedback.PlayCaught();
            else feedback.PlayLost();
            if (hud != null) hud.ShowResult(ResultMessage, awarded);
        }

        public void Cancel()
        {
            if (Active != this) return;
            Active = null;
            Phase = FishingPhase.Idle;
            touchHeld = false;
            pullHeld = false;
            lastPullInput = false;
            if (player != null) player.SetControlsLocked(false);
            if (hud != null) hud.Hide(this);
            if (visuals != null) Destroy(visuals.gameObject);
            player = null;
            visuals = null;
        }

        private void OnDisable() => Cancel();
        private void OnDestroy() => Cancel();
    }
}
