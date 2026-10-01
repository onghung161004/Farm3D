using NUnit.Framework;
using UnityEngine;

namespace FarmRestoration.Tests.EditMode
{
    public sealed class PlayerMovementMathTests
    {
        [Test]
        public void NormalizePlanarInput_DiagonalInput_HasUnitMagnitude()
        {
            Vector2 result = PlayerMovementMath.NormalizePlanarInput(new Vector2(1f, 1f));

            Assert.That(result.magnitude, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void NormalizePlanarInput_InputWithinUnitCircle_IsUnchanged()
        {
            Vector2 input = new Vector2(0.5f, -0.25f);

            Vector2 result = PlayerMovementMath.NormalizePlanarInput(input);

            Assert.That(result, Is.EqualTo(input));
        }

        [Test]
        public void GetCameraRelativePlanarDirection_ZeroInput_ReturnsZero()
        {
            Vector3 result = PlayerMovementMath.GetCameraRelativePlanarDirection(
                Vector2.zero,
                Vector3.forward,
                Vector3.right);

            Assert.That(result, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void GetCameraRelativePlanarDirection_ForwardInput_UsesPlanarCameraForward()
        {
            Vector3 result = PlayerMovementMath.GetCameraRelativePlanarDirection(
                Vector2.up,
                new Vector3(0f, 2f, 4f),
                Vector3.right);

            Assert.That(result, Is.EqualTo(Vector3.forward));
        }
    }
}
