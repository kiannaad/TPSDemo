using System;
using Animancer;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CGame.Animation
{
    [CreateAssetMenu(fileName = "TwoDimensionalAnimationBlendAsset", menuName = "CGame/Animation/2D Animation Blend Asset")]
    public class TwoDimensionalAnimationBlendAsset : AnimationAssetBase
    {
        [Serializable]
        public class BlendChild
        {
            [SerializeField] private AnimationClipAsset clipAsset;
            [SerializeField] private Vector2 threshold;
            [SerializeField] private float speed = 1f;
            [SerializeField] private bool synchronize = true;

            public AnimationClipAsset ClipAsset
            {
                get => clipAsset;
                set => clipAsset = value;
            }

            public Vector2 Threshold
            {
                get => threshold;
                set => threshold = value;
            }

            public float Speed
            {
                get => speed;
                set => speed = value;
            }

            public bool Synchronize
            {
                get => synchronize;
                set => synchronize = value;
            }
        }

        [SerializeField] private MixerTransition2D.MixerType mixerType = MixerTransition2D.MixerType.Directional;
        [SerializeField] private Vector2 defaultParameter;
        [SerializeField] private BlendChild[] children = Array.Empty<BlendChild>();

        public MixerTransition2D.MixerType MixerType
        {
            get => mixerType;
            set => mixerType = value;
        }

        public Vector2 DefaultParameter
        {
            get => defaultParameter;
            set => defaultParameter = value;
        }

        public BlendChild[] Children
        {
            get => children;
            set => children = value ?? Array.Empty<BlendChild>();
        }

        public override AnimationClip MainClip
            => children != null && children.Length > 0 ? children[0]?.ClipAsset?.MainClip : null;

        public override bool IsValid
        {
            get
            {
                if (children == null || children.Length == 0)
                {
                    return false;
                }

                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i]?.ClipAsset == null || !children[i].ClipAsset.IsValid)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public override ITransition CreateTransition()
        {
            return CreateMixerTransition();
        }

        public MixerTransition2D CreateMixerTransition()
        {
            int childCount = children?.Length ?? 0;
            var animations = new Object[childCount];
            var thresholds = new Vector2[childCount];
            var speeds = new float[childCount];
            var synchronizeChildren = new bool[childCount];

            for (int i = 0; i < childCount; i++)
            {
                BlendChild child = children[i];
                animations[i] = child?.ClipAsset?.MainClip;
                thresholds[i] = child != null ? child.Threshold : default;
                speeds[i] = child != null ? child.Speed : 1f;
                synchronizeChildren[i] = child == null || child.Synchronize;
            }

            var transition = new MixerTransition2D();
            transition.Type = mixerType;
            transition.DefaultParameter = defaultParameter;
            transition.Animations = animations;
            transition.Thresholds = thresholds;
            transition.Speeds = speeds;
            transition.SynchronizeChildren = synchronizeChildren;
            return transition;
        }
    }
}
