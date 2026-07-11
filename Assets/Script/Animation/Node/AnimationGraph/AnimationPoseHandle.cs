using UnityEngine.Playables;

namespace CGame.Animation
{
    public readonly struct AnimationPoseHandle
    {
        public AnimationPoseHandle(Playable playable, float weight, long evaluateFrameId, string source)
        {
            Playable = playable;
            Weight = weight;
            EvaluateFrameId = evaluateFrameId;
            Source = source ?? string.Empty;
        }

        public Playable Playable { get; }
        public float Weight { get; }
        public long EvaluateFrameId { get; }
        public string Source { get; }
        public bool IsValid => Playable.IsValid() && Weight > 0f;

        public static AnimationPoseHandle Invalid(long evaluateFrameId, string source)
        {
            return new AnimationPoseHandle(Playable.Null, 0f, evaluateFrameId, source);
        }

        public static implicit operator Playable(AnimationPoseHandle handle) => handle.Playable;
    }
}
