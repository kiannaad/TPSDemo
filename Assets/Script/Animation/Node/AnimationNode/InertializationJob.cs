using UnityEngine;
using UnityEngine.Animations;

namespace CGame.Animation
{
    public struct InertializationJob : IAnimationJob
    {
        public TransformStreamHandle TransformHandle;
        public Vector3 PreviousOutputPosition;
        public Quaternion PreviousOutputRotation;
        public Vector3 PositionOffset;
        public Quaternion RotationOffset;
        public float Duration;
        public float Elapsed;
        public float Weight;
        public float MinWeight;
        public bool HasPreviousOutput;
        public bool Trigger;
        public bool Enabled;
        public bool IsActive;

        public void ProcessAnimation(AnimationStream stream)
        {
            if (!TransformHandle.IsValid(stream))
            {
                return;
            }

            Vector3 sourcePosition = TransformHandle.GetLocalPosition(stream);
            Quaternion sourceRotation = TransformHandle.GetLocalRotation(stream);
            if (Trigger)
            {
                Trigger = false;
                if (Enabled && HasPreviousOutput && Duration > 0f && Weight >= MinWeight)
                {
                    PositionOffset = PreviousOutputPosition - sourcePosition;
                    RotationOffset = PreviousOutputRotation * Quaternion.Inverse(sourceRotation);
                    Elapsed = 0f;
                    IsActive = true;
                }
                else
                {
                    IsActive = false;
                }
            }

            Vector3 outputPosition = sourcePosition;
            Quaternion outputRotation = sourceRotation;
            if (Enabled && IsActive)
            {
                Elapsed += Mathf.Max(0f, stream.deltaTime);
                float decay = 1f - Mathf.Clamp01(Elapsed / Duration);
                outputPosition += PositionOffset * (decay * Weight);
                outputRotation = Quaternion.Slerp(Quaternion.identity, RotationOffset, decay * Weight) * sourceRotation;
                if (decay <= 0f)
                {
                    IsActive = false;
                }
            }

            TransformHandle.SetLocalPosition(stream, outputPosition);
            TransformHandle.SetLocalRotation(stream, outputRotation);
            PreviousOutputPosition = outputPosition;
            PreviousOutputRotation = outputRotation;
            HasPreviousOutput = true;
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }
    }
}
