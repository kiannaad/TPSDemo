using Animancer;
using UnityEngine;

namespace CGame.Animation
{
    [CreateAssetMenu(fileName = "AnimationSequenceAsset", menuName = "CGame/Animation/Animation Sequence Asset")]
    public class AnimationSequenceAsset : AnimationAssetBase
    {
        [System.Serializable]
        public class SequenceEntry
        {
            [SerializeField] private AnimationClipAsset clipAsset;
            [SerializeField] private float speed = 1f;

            public AnimationClipAsset ClipAsset
            {
                get => clipAsset;
                set => clipAsset = value;
            }

            public float Speed
            {
                get => speed;
                set => speed = value;
            }
        }

        [SerializeField, Min(0f)] private float fadeDuration = AnimancerGraph.DefaultFadeDuration;
        [SerializeField] private SequenceEntry[] clips = System.Array.Empty<SequenceEntry>();

        public float FadeDuration
        {
            get => fadeDuration;
            set => fadeDuration = Mathf.Max(0f, value);
        }

        public SequenceEntry[] Clips
        {
            get => clips;
            set => clips = value ?? System.Array.Empty<SequenceEntry>();
        }

        public override AnimationClip MainClip
            => clips != null && clips.Length > 0 ? clips[0]?.ClipAsset?.MainClip : null;

        public override bool IsValid
        {
            get
            {
                if (clips == null || clips.Length == 0)
                {
                    return false;
                }

                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i]?.ClipAsset == null || !clips[i].ClipAsset.IsValid)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public override ITransition CreateTransition()
        {
            return CreateSequenceTransition();
        }

        public ClipTransitionSequence CreateSequenceTransition()
        {
            var transition = new ClipTransitionSequence
            {
                Clip = MainClip,
                FadeDuration = fadeDuration,
            };

            if (clips == null || clips.Length == 0)
            {
                transition.Others = System.Array.Empty<ClipTransition>();
                return transition;
            }

            ApplyEntryToTransition(clips[0], transition);

            var others = new ClipTransition[Mathf.Max(0, clips.Length - 1)];
            for (int i = 1; i < clips.Length; i++)
            {
                var clipTransition = new ClipTransition();
                ApplyEntryToTransition(clips[i], clipTransition);
                others[i - 1] = clipTransition;
            }

            transition.Others = others;
            return transition;
        }

        private void ApplyEntryToTransition(SequenceEntry entry, ClipTransition transition)
        {
            AnimationClipAsset clipAsset = entry?.ClipAsset;
            transition.Clip = clipAsset?.MainClip;
            transition.FadeDuration = fadeDuration;
            transition.Speed = clipAsset != null ? clipAsset.Speed * (entry?.Speed ?? 1f) : 1f;
        }
    }
}
