using UnityEngine;

namespace CGame
{
    public sealed class FirstPersonCameraAnchor : MonoBehaviour, IFirstPersonCameraTarget, IFirstPersonCameraLocomotionSource
    {
        [SerializeField]
        private float localEyeHeight = 1.6f;

        [SerializeField]
        private float localEyeForward = 0.4f;

        private CharacterPhysicsMotor characterMotor;

        public Vector3 Position => transform.TransformPoint(0f, localEyeHeight, localEyeForward);
        public Quaternion Rotation => transform.rotation;
        public bool IsValid => this != null && gameObject.activeInHierarchy;
        public float LocalEyeHeight => localEyeHeight;
        public float LocalEyeForward => localEyeForward;
        public CameraLocomotionSample LocomotionSample
        {
            get
            {
                if (characterMotor == null)
                {
                    return CameraLocomotionSample.Idle;
                }

                Vector3 velocity = characterMotor.Velocity;
                float verticalSpeed = Vector3.Dot(velocity, characterMotor.CharacterUp);
                float horizontalSpeed = Vector3.ProjectOnPlane(velocity, characterMotor.CharacterUp).magnitude;
                return new CameraLocomotionSample(
                    horizontalSpeed,
                    verticalSpeed,
                    characterMotor.GroundingStatus.IsStableOnGround);
            }
        }

        private void Awake()
        {
            characterMotor = GetComponentInParent<CharacterPhysicsMotor>();
        }
    }
}
