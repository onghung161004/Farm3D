using UnityEngine;

namespace FarmRestoration
{
    public static class PlayerMovementMath
    {
        public static Vector2 NormalizePlanarInput(Vector2 input)
        {
            return input.sqrMagnitude > 1f ? input.normalized : input;
        }

        public static Vector3 GetCameraRelativePlanarDirection(
            Vector2 input,
            Vector3 cameraForward,
            Vector3 cameraRight)
        {
            Vector3 planarForward = Vector3.ProjectOnPlane(cameraForward, Vector3.up).normalized;
            Vector3 planarRight = Vector3.ProjectOnPlane(cameraRight, Vector3.up).normalized;
            return (planarForward * input.y) + (planarRight * input.x);
        }
    }
}
