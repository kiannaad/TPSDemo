using UnityEngine;

namespace CGame.Animation
{
    public readonly struct AnimationRootMotionDelta
    {
        public AnimationRootMotionDelta(Vector3 positionDelta, Quaternion rotationDelta, float sourceWeight)
        {
            PositionDelta = positionDelta;
            RotationDelta = rotationDelta;
            SourceWeight = Mathf.Clamp01(sourceWeight);
        }

        public Vector3 PositionDelta { get; }
        public Quaternion RotationDelta { get; }
        public float SourceWeight { get; }
        public bool IsValid => SourceWeight > 0f;

        public static AnimationRootMotionDelta None => new AnimationRootMotionDelta(Vector3.zero, Quaternion.identity, 0f);
    }
}
