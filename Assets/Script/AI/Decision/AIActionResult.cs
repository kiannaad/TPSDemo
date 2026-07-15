using UnityEngine;

namespace CGame
{
    public readonly struct AIActionResult
    {
        public AIActionResult(
            AIActionKind kind,
            AIActionStatus status,
            string reason,
            Vector3 movementDirection)
        {
            Kind = kind;
            Status = status;
            Reason = reason ?? string.Empty;
            MovementDirection = movementDirection;
        }

        public AIActionKind Kind { get; }
        public AIActionStatus Status { get; }
        public string Reason { get; }
        public Vector3 MovementDirection { get; }
    }
}
