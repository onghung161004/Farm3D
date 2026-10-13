using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmRestoration
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 4.5f;
        [SerializeField, Min(0f)] private float rotationSharpness = 14f;
        [SerializeField, Min(0.01f)] private float acceleration = 18f;
        [SerializeField, Min(0.01f)] private float deceleration = 24f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private Camera movementCamera;

        private CharacterController characterController;
        private float verticalVelocity;
        private Vector3 planarVelocity;
        private Vector2 mobileInput;
        private readonly PlayerMapBoundary mapBoundary = new PlayerMapBoundary();
        private float nextBoundaryLookup;

        public bool IsMoving { get; private set; }
        public bool ControlsLocked { get; private set; }

        public void SetControlsLocked(bool locked)
        {
            ControlsLocked = locked;
            if (locked) { planarVelocity = Vector3.zero; IsMoving = false; }
        }

        public void SetMobileInput(Vector2 input)
        {
            mobileInput = Vector2.ClampMagnitude(input, 1f);
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (characterController == null)
            {
                Debug.LogError("PlayerMovement requires a CharacterController.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            Vector2 input = ControlsLocked ? Vector2.zero : PlayerMovementMath.NormalizePlanarInput(
                ReadKeyboardInput() + mobileInput);
            Vector3 movement = GetCameraRelativeMovement(input);
            Vector3 targetVelocity = movement * moveSpeed;
            float speedChange = movement.sqrMagnitude > 0.0001f ? acceleration : deceleration;
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, speedChange * Time.deltaTime);
            IsMoving = planarVelocity.sqrMagnitude > 0.01f;

            ResolveMapBoundary();
            Vector3 proposed = transform.position + planarVelocity * Time.deltaTime;
            Vector3 constrained = mapBoundary.Constrain(proposed, characterController.radius + characterController.skinWidth);
            characterController.Move(constrained - transform.position);
            ApplyGravity();
            if (mapBoundary.TryRecover(transform.position, out Vector3 recovery))
            {
                if (FishingSession.Active != null) FishingSession.Active.Cancel();
                characterController.enabled = false;
                transform.position = recovery;
                characterController.enabled = true;
                verticalVelocity = 0f;
                planarVelocity = Vector3.zero;
                mobileInput = Vector2.zero;
                IsMoving = false;
                return;
            }
            RotateTowards(planarVelocity);
        }

        private void ResolveMapBoundary()
        {
            if (mapBoundary.HasTerrain || Time.unscaledTime < nextBoundaryLookup) return;
            nextBoundaryLookup = Time.unscaledTime + 1f;
            MeshCollider ground = GameObject.Find("WalkableValleyTerrain")?.GetComponent<MeshCollider>();
            if (ground != null) mapBoundary.Configure(ground);
        }

        public void ConfigureCamera(Camera cameraToFollow)
        {
            movementCamera = cameraToFollow;
        }

        private static Vector2 ReadKeyboardInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            float horizontal = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            float vertical = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(horizontal, vertical);
        }

        private Vector3 GetCameraRelativeMovement(Vector2 input)
        {
            Transform referenceTransform = movementCamera != null ? movementCamera.transform : transform;
            return PlayerMovementMath.GetCameraRelativePlanarDirection(
                input,
                referenceTransform.forward,
                referenceTransform.right);
        }

        private void ApplyGravity()
        {
            verticalVelocity = characterController.isGrounded ? -2f : verticalVelocity + (gravity * Time.deltaTime);
            characterController.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
        }

        private void RotateTowards(Vector3 movement)
        {
            if (movement.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
            float blend = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, blend);
        }
    }
}
