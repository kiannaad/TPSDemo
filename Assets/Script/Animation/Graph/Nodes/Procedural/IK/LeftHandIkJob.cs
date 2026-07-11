using UnityEngine;
using UnityEngine.Animations;

namespace CGame.Animation
{
    public struct LeftHandIkJob : IAnimationJob
    {
        public TransformStreamHandle LeftHand;
        public Vector3 TargetPosition;
        public Quaternion TargetRotation;
        public float Weight;

        public void ProcessAnimation(AnimationStream stream)
        {
            if (!LeftHand.IsValid(stream) || Weight <= 0f)
            {
                return;
            }

            Vector3 position = LeftHand.GetPosition(stream);
            Quaternion rotation = LeftHand.GetRotation(stream);
            LeftHand.SetPosition(stream, Vector3.Lerp(position, TargetPosition, Weight));
            LeftHand.SetRotation(stream, Quaternion.Slerp(rotation, TargetRotation, Weight));
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }
    }
}
