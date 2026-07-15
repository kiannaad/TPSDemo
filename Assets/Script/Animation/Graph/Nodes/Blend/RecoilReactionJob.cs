using UnityEngine;
using UnityEngine.Animations;

namespace CGame.Animation
{
    public struct RecoilReactionJob : IAnimationJob
    {
        public TransformStreamHandle Spine;
        public TransformStreamHandle Chest;
        public TransformStreamHandle UpperChest;
        public float Pitch;

        public void ProcessAnimation(AnimationStream stream)
        {
            Apply(stream, Spine, 0.15f);
            Apply(stream, Chest, 0.35f);
            Apply(stream, UpperChest, 0.5f);
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        private void Apply(AnimationStream stream, TransformStreamHandle handle, float distribution)
        {
            if (!handle.IsValid(stream) || Mathf.Approximately(Pitch, 0f)) return;
            Quaternion recoil = Quaternion.Euler(Pitch * distribution, 0f, 0f);
            handle.SetLocalRotation(stream, handle.GetLocalRotation(stream) * recoil);
        }
    }
}
