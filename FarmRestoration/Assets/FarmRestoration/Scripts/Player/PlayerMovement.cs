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

        public bool IsMoving { get; private set; }

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
            Vector2 input = PlayerMovementMath.NormalizePlanarInput(ReadKeyboardInput());
            Vector3 movement = GetCameraRelativeMovement(input);
            Vector3 targetVelocity = movement * moveSpeed;
            float speedChange = movement.sqrMagnitude > 0.0001f ? acceleration : deceleration;
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, speedChange * Time.deltaTime);
            IsMoving = planarVelocity.sqrMagnitude > 0.01f;

            characterController.Move(planarVelocity * Time.deltaTime);
            ApplyGravity();
            RotateTowards(planarVelocity);
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
