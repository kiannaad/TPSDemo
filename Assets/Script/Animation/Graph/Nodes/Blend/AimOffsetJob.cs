using UnityEngine;
using UnityEngine.Animations;

namespace CGame.Animation
{
    public struct AimOffsetJob : IAnimationJob
    {
        public TransformStreamHandle Spine;
        public TransformStreamHandle Chest;
        public TransformStreamHandle UpperChest;
        public float Pitch;
        public float Yaw;
        public float Weight;

        public void ProcessAnimation(AnimationStream stream)
        {
            if (Weight <= 0f)
            {
                return;
            }

            Apply(stream, Spine, 0.2f);
            Apply(stream, Chest, 0.35f);
            Apply(stream, UpperChest, 0.45f);
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        private void Apply(AnimationStream stream, TransformStreamHandle handle, float distribution)
        {
            if (!handle.IsValid(stream))
            {
                return;
            }

            Quaternion additive = Quaternion.Euler(Pitch * distribution * Weight, Yaw * distribution * Weight, 0f);
            handle.SetLocalRotation(stream, handle.GetLocalRotation(stream) * additive);
        }
    }
}
