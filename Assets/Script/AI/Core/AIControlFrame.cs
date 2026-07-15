using UnityEngine;

namespace CGame
{
    public readonly struct AIControlFrame
    {
        public AIControlFrame(
            Vector3 movementDirection,
            Vector3 aimDirection,
            bool jumpRequested,
            bool fireRequested,
            bool reloadRequested)
        {
            MovementDirection = Vector3.ClampMagnitude(movementDirection, 1f);
            AimDirection = aimDirection.sqrMagnitude > 0.000001f
                ? aimDirection.normalized
                : Vector3.zero;
            JumpRequested = jumpRequested;
            FireRequested = fireRequested;
            ReloadRequested = reloadRequested;
        }

        public Vector3 MovementDirection { get; }
        public Vector3 AimDirection { get; }
        public bool JumpRequested { get; }
        public bool FireRequested { get; }
        public bool ReloadRequested { get; }
    }
}
