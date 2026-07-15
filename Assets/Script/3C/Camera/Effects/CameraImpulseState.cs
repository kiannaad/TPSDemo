using UnityEngine;

namespace CGame
{
    public sealed class CameraImpulseState
    {
        public const float MaxLocalPosition = 0.05f;
        public const float MaxLocalRotation = 3f;

        private Vector3 localPosition;
        private Vector3 localEulerAngles;
        private float positionRecoverySpeed;
        private float rotationRecoverySpeed;

        public void Apply(CameraImpulseRequest request)
        {
            localPosition = Vector3.ClampMagnitude(
                localPosition + request.LocalPosition,
                MaxLocalPosition);
            localEulerAngles = Vector3.ClampMagnitude(
                localEulerAngles + request.LocalEulerAngles,
                MaxLocalRotation);
            positionRecoverySpeed = request.PositionRecoverySpeed;
            rotationRecoverySpeed = request.RotationRecoverySpeed;
        }

        public CameraPoseDelta Advance(float deltaTime)
        {
            CameraPoseDelta delta = CreateDelta();
            if (deltaTime > 0f)
            {
                localPosition = Vector3.MoveTowards(
                    localPosition,
                    Vector3.zero,
                    positionRecoverySpeed * deltaTime);
                localEulerAngles = Vector3.MoveTowards(
                    localEulerAngles,
                    Vector3.zero,
                    rotationRecoverySpeed * deltaTime);
            }

            return delta;
        }

        public void Reset()
        {
            localPosition = Vector3.zero;
            localEulerAngles = Vector3.zero;
            positionRecoverySpeed = 0f;
            rotationRecoverySpeed = 0f;
        }

        private CameraPoseDelta CreateDelta()
        {
            bool hasValue = localPosition.sqrMagnitude > 0.00000001f ||
                localEulerAngles.sqrMagnitude > 0.00000001f;
            if (!hasValue)
            {
                return CameraPoseDelta.None;
            }

            Quaternion rotation = Quaternion.Euler(localEulerAngles);
            float angle = Quaternion.Angle(Quaternion.identity, rotation);
            if (angle > MaxLocalRotation)
            {
                rotation = Quaternion.Slerp(
                    Quaternion.identity,
                    rotation,
                    MaxLocalRotation / angle);
            }

            return new CameraPoseDelta(localPosition, rotation, 1f);
        }
    }
}
