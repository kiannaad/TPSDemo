using UnityEngine;
using UnityEngine.Animations;

namespace CGame.Animation
{
    public struct RootMotionCaptureJob : IAnimationJob
    {
        public Vector3 PositionDelta;
        public Quaternion RotationDelta;

        public void ProcessAnimation(AnimationStream stream)
        {
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
            PositionDelta = stream.velocity * stream.deltaTime;
            Vector3 angularDelta = stream.angularVelocity * (Mathf.Rad2Deg * stream.deltaTime);
            RotationDelta = Quaternion.Euler(angularDelta);
        }
    }
}
