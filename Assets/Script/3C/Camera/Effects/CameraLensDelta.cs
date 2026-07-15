using UnityEngine;

namespace CGame
{
    public readonly struct CameraLensDelta
    {
        public CameraLensDelta(float fieldOfView, float weight)
        {
            FieldOfView = fieldOfView;
            Weight = Mathf.Clamp01(weight);
        }

        public float FieldOfView { get; }
        public float Weight { get; }

        public static CameraLensDelta None => new CameraLensDelta(0f, 0f);
    }
}
