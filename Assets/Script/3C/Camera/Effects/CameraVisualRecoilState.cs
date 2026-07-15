using UnityEngine;

namespace CGame
{
    public sealed class CameraVisualRecoilState
    {
        private const float MaxCameraPosition = 0.08f;
        private const float MaxCameraRotation = 5f;
        private const float MaxViewModelPosition = 0.15f;
        private const float MaxViewModelRotation = 8f;

        private Vector3 cameraPosition;
        private Vector3 cameraEulerAngles;
        private Vector3 viewModelPosition;
        private Vector3 viewModelEulerAngles;
        private float positionRecoverySpeed;
        private float rotationRecoverySpeed;

        public void Apply(WeaponRecoilRequest request)
        {
            cameraPosition = ClampMagnitude(cameraPosition + request.CameraLocalPosition, MaxCameraPosition);
            cameraEulerAngles = ClampMagnitude(cameraEulerAngles + request.CameraLocalEulerAngles, MaxCameraRotation);
            viewModelPosition = ClampMagnitude(viewModelPosition + request.ViewModelLocalPosition, MaxViewModelPosition);
            viewModelEulerAngles = ClampMagnitude(viewModelEulerAngles + request.ViewModelLocalEulerAngles, MaxViewModelRotation);
            positionRecoverySpeed = request.VisualPositionRecoverySpeed;
            rotationRecoverySpeed = request.VisualRotationRecoverySpeed;
        }

        public CameraVisualRecoilFrame Advance(float deltaTime)
        {
            CameraVisualRecoilFrame frame = new CameraVisualRecoilFrame(
                CreateDelta(cameraPosition, cameraEulerAngles, MaxCameraRotation),
                CreateDelta(viewModelPosition, viewModelEulerAngles, MaxViewModelRotation));
            if (deltaTime > 0f)
            {
                float positionStep = positionRecoverySpeed * deltaTime;
                float rotationStep = rotationRecoverySpeed * deltaTime;
                cameraPosition = Vector3.MoveTowards(cameraPosition, Vector3.zero, positionStep);
                viewModelPosition = Vector3.MoveTowards(viewModelPosition, Vector3.zero, positionStep);
                cameraEulerAngles = Vector3.MoveTowards(cameraEulerAngles, Vector3.zero, rotationStep);
                viewModelEulerAngles = Vector3.MoveTowards(viewModelEulerAngles, Vector3.zero, rotationStep);
            }

            return frame;
        }

        public void Reset()
        {
            cameraPosition = Vector3.zero;
            cameraEulerAngles = Vector3.zero;
            viewModelPosition = Vector3.zero;
            viewModelEulerAngles = Vector3.zero;
            positionRecoverySpeed = 0f;
            rotationRecoverySpeed = 0f;
        }

        private static CameraPoseDelta CreateDelta(
            Vector3 position,
            Vector3 eulerAngles,
            float maxRotationDegrees)
        {
            bool hasValue = position.sqrMagnitude > 0.00000001f || eulerAngles.sqrMagnitude > 0.00000001f;
            Quaternion rotation = Quaternion.Euler(eulerAngles);
            float rotationAngle = Quaternion.Angle(Quaternion.identity, rotation);
            if (rotationAngle > maxRotationDegrees)
            {
                rotation = Quaternion.Slerp(
                    Quaternion.identity,
                    rotation,
                    maxRotationDegrees / rotationAngle);
            }

            return hasValue
                ? new CameraPoseDelta(position, rotation, 1f)
                : CameraPoseDelta.None;
        }

        private static Vector3 ClampMagnitude(Vector3 value, float maxMagnitude)
        {
            return value.sqrMagnitude > maxMagnitude * maxMagnitude
                ? value.normalized * maxMagnitude
                : value;
        }
    }
}
