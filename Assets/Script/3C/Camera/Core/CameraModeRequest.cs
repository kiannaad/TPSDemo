using System;

namespace CGame
{
    public sealed class CameraModeRequest
    {
        public CameraModeRequest(
            CameraMode mode,
            ICameraModeTarget target,
            CameraModeTransition transition,
            float duration)
        {
            if (mode == CameraMode.GameplayFirstPerson)
            {
                throw new ArgumentException("GameplayFirstPerson is the fallback and cannot be requested.", nameof(mode));
            }

            Mode = mode;
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Transition = transition;
            Duration = Math.Max(0f, duration);
        }

        public CameraMode Mode { get; }
        public ICameraModeTarget Target { get; }
        public CameraModeTransition Transition { get; }
        public float Duration { get; }
        public int Priority => (int)Mode;
    }
}
