using System;
using UnityEngine;

namespace CGame.Animation
{
    [CreateAssetMenu(fileName = "TwoDimensionalAnimationBlendAsset", menuName = "CGame/Animation/2D Animation Blend Asset")]
    public class TwoDimensionalAnimationBlendAsset : AnimationAssetBase
    {
        public enum BlendType
        {
            Cartesian,
            Directional,
        }

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

        [SerializeField] private BlendType mixerType = BlendType.Directional;
        [SerializeField] private Vector2 defaultParameter;
        [SerializeField] private BlendChild[] children = Array.Empty<BlendChild>();

        public BlendType MixerType
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

    }
}
