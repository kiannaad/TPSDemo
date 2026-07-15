using UnityEngine;

namespace CGame
{
    public readonly struct CameraImpulseRequest
    {
        public CameraImpulseRequest(
            Vector3 localPosition,
            Vector3 localEulerAngles,
            float positionRecoverySpeed,
            float rotationRecoverySpeed)
        {
            LocalPosition = localPosition;
            LocalEulerAngles = localEulerAngles;
            PositionRecoverySpeed = Mathf.Max(0f, positionRecoverySpeed);
            RotationRecoverySpeed = Mathf.Max(0f, rotationRecoverySpeed);
        }

        public Vector3 LocalPosition { get; }
        public Vector3 LocalEulerAngles { get; }
        public float PositionRecoverySpeed { get; }
        public float RotationRecoverySpeed { get; }
    }
}
