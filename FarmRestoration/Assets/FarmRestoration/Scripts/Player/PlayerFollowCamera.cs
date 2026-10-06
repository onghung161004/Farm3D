using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmRestoration
{
    [RequireComponent(typeof(Camera))]
    public sealed class PlayerFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 5.5f, -9.5f);
        [SerializeField, Min(0f)] private float lookHeight = 3.3f;
        [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.16f;
        [SerializeField, Min(0f)] private float rotationSharpness = 12f;
        [SerializeField, Range(0f, 80f)] private float minimumPitch = 20f;
        [SerializeField, Range(1f, 89f)] private float maximumPitch = 65f;
        [SerializeField, Min(1f)] private float minimumDistance = 4.5f;
        [SerializeField, Min(1f)] private float maximumDistance = 16f;
        [SerializeField, Min(0f)] private float orbitSensitivity = 0.15f;
        [SerializeField, Min(0f)] private float zoomSensitivity = 0.01f;

        private Vector3 positionVelocity;
        private float yaw;
        private float pitch;
        private float distance;
        private bool orbitInitialized;

        private void Awake()
        {
            // A scene left open in the Editor can retain the previously serialized camera offset.
            if ((offset - new Vector3(0f, 6.5f, -7.5f)).sqrMagnitude < 0.0001f)
            {
                offset = new Vector3(0f, 5.5f, -9.5f);
                lookHeight = 3.3f;
            }

            InitializeOrbit();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (!orbitInitialized) InitializeOrbit();
            ReadOrbitInput();

            Vector3 focus = target.position + Vector3.up * lookHeight;
            Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPosition = focus + orbitRotation * (Vector3.back * distance);
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref positionVelocity,
                positionSmoothTime);

            // Never let smoothing or a moved target put the camera below the character.
            float minimumCameraY = focus.y + 0.25f;
            if (transform.position.y < minimumCameraY)
            {
                Vector3 safePosition = transform.position;
                safePosition.y = minimumCameraY;
                transform.position = safePosition;
                positionVelocity.y = 0f;
            }

            Vector3 lookDirection = focus - transform.position;
            if (lookDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            float blend = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, blend);
        }

        private void InitializeOrbit()
        {
            float horizontalDistance = new Vector2(offset.x, offset.z).magnitude;
            float heightAboveFocus = offset.y - lookHeight;
            float lowerPitch = Mathf.Clamp(minimumPitch, 1f, 80f);
            float upperPitch = Mathf.Clamp(Mathf.Max(maximumPitch, lowerPitch), lowerPitch, 89f);
            float nearest = Mathf.Max(1f, minimumDistance);
            float maximumZoom = Mathf.Max(maximumDistance, nearest);

            yaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(Mathf.Atan2(heightAboveFocus, horizontalDistance) * Mathf.Rad2Deg, lowerPitch, upperPitch);
            distance = Mathf.Clamp(new Vector2(horizontalDistance, heightAboveFocus).magnitude, nearest, maximumZoom);
            orbitInitialized = true;

            if (target == null) return;
            Vector3 focus = target.position + Vector3.up * lookHeight;
            transform.position = focus + Quaternion.Euler(pitch, yaw, 0f) * (Vector3.back * distance);
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
            positionVelocity = Vector3.zero;
        }

        private void ReadOrbitInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * orbitSensitivity;
                float lowerPitch = Mathf.Clamp(minimumPitch, 1f, 80f);
                float upperPitch = Mathf.Clamp(Mathf.Max(maximumPitch, lowerPitch), lowerPitch, 89f);
                pitch = Mathf.Clamp(pitch - delta.y * orbitSensitivity, lowerPitch, upperPitch);
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.001f)
            {
                float nearest = Mathf.Max(1f, minimumDistance);
                distance = Mathf.Clamp(distance - scroll * zoomSensitivity, nearest, Mathf.Max(maximumDistance, nearest));
            }
        }

        public void ConfigureTarget(Transform newTarget)
        {
            target = newTarget;
            orbitInitialized = false;
        }
    }
}
