using System;
using UnityEngine;

namespace CGame
{
    public readonly struct AIActionRequest
    {
        public AIActionRequest(
            AIActionKind kind,
            Vector3 targetPosition,
            Vector3 aimDirection,
            double createdAt,
            float minimumCommitment,
            float maximumDuration)
        {
            if (double.IsNaN(createdAt)
                || double.IsInfinity(createdAt)
                || minimumCommitment < 0f
                || maximumDuration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(createdAt));
            }

            Kind = kind;
            TargetPosition = targetPosition;
            AimDirection = aimDirection.sqrMagnitude > 0.000001f
                ? aimDirection.normalized
                : Vector3.zero;
            CreatedAt = createdAt;
            MinimumCommitment = minimumCommitment;
            MaximumDuration = maximumDuration;
        }

        public AIActionKind Kind { get; }
        public Vector3 TargetPosition { get; }
        public Vector3 AimDirection { get; }
        public double CreatedAt { get; }
        public float MinimumCommitment { get; }
        public float MaximumDuration { get; }
        public double CommitmentUntil => CreatedAt + MinimumCommitment;
        public double ExpiresAt => CreatedAt + MaximumDuration;
    }
}
