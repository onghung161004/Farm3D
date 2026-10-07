using UnityEngine;

namespace FarmRestoration
{
    [DisallowMultipleComponent]
    public sealed class FootstepSurface : MonoBehaviour
    {
        public enum Kind { Grass, Wood }

        [SerializeField] private Kind surface = Kind.Grass;
        public Kind Surface => surface;

        public void Configure(Kind value) => surface = value;
    }
}
