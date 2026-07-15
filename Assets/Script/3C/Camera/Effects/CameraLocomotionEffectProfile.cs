using UnityEngine;

namespace CGame
{
    [CreateAssetMenu(fileName = "CameraLocomotionEffectProfile", menuName = "CGame/Camera/Locomotion Effect Profile")]
    public sealed class CameraLocomotionEffectProfile : ScriptableObject
    {
        [SerializeField, Range(0f, 1f)] private float stanceWeight = 1f;
        [SerializeField, Range(-0.05f, 0f)] private float airborneEyeOffset = -0.018f;
        [SerializeField, Min(0f)] private float stanceResponse = 10f;
        [SerializeField, Min(0f)] private float movementResponse = 12f;
        [SerializeField, Min(0f)] private float walkSpeed = 2f;
        [SerializeField, Min(0.01f)] private float sprintSpeed = 6f;
        [SerializeField, Range(0f, 1f)] private float bobWeight = 0.65f;
        [SerializeField] private Vector3 walkBobPosition = new Vector3(0.008f, 0.012f, 0f);
        [SerializeField] private Vector3 sprintBobPosition = new Vector3(0.014f, 0.022f, 0f);
        [SerializeField, Min(0f)] private float walkBobFrequency = 1.6f;
        [SerializeField, Min(0f)] private float sprintBobFrequency = 2.3f;
        [SerializeField, Range(0f, 1f)] private float swayWeight = 0.65f;
        [SerializeField] private Vector3 swayPositionAmplitude = new Vector3(0.004f, 0f, 0f);
        [SerializeField] private Vector3 swayRotationAmplitude = new Vector3(0.12f, 0.08f, 0.35f);
        [SerializeField, Range(0f, 1f)] private float breathWeight = 0.7f;
        [SerializeField, Min(0f)] private float breathFrequency = 0.22f;
        [SerializeField, Min(0f)] private float breathPositionAmplitude = 0.0035f;
        [SerializeField, Min(0f)] private float breathPitchAmplitude = 0.08f;
        [SerializeField, Min(0.001f)] private float maxLocalPosition = 0.05f;
        [SerializeField, Range(0.01f, 5f)] private float maxLocalRotation = 1f;

        public float StanceWeight => stanceWeight;
        public float AirborneEyeOffset => airborneEyeOffset;
        public float StanceResponse => stanceResponse;
        public float MovementResponse => movementResponse;
        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => Mathf.Max(walkSpeed + 0.01f, sprintSpeed);
        public float BobWeight => bobWeight;
        public Vector3 WalkBobPosition => walkBobPosition;
        public Vector3 SprintBobPosition => sprintBobPosition;
        public float WalkBobFrequency => walkBobFrequency;
        public float SprintBobFrequency => sprintBobFrequency;
        public float SwayWeight => swayWeight;
        public Vector3 SwayPositionAmplitude => swayPositionAmplitude;
        public Vector3 SwayRotationAmplitude => swayRotationAmplitude;
        public float BreathWeight => breathWeight;
        public float BreathFrequency => breathFrequency;
        public float BreathPositionAmplitude => breathPositionAmplitude;
        public float BreathPitchAmplitude => breathPitchAmplitude;
        public float MaxLocalPosition => maxLocalPosition;
        public float MaxLocalRotation => maxLocalRotation;
    }
}
