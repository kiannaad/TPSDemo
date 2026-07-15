using UnityEngine;

namespace CGame
{
    public sealed class CameraModeTargetState : ICameraModeTarget
    {
        private const float DefaultFieldOfView = 60f;

        public CameraPose Pose { get; private set; }
        public float FieldOfView { get; private set; } = DefaultFieldOfView;
        public bool IsValid { get; private set; } = true;

        public void Updating(CameraPose pose, float fieldOfView)
        {
            if (!IsValid)
            {
                return;
            }

            Pose = pose;
            FieldOfView = Mathf.Clamp(fieldOfView, 1f, 179f);
        }

        public void Invalidating()
        {
            IsValid = false;
        }
    }
}
