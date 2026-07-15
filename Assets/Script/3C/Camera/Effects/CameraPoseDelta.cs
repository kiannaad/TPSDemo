using UnityEngine;

namespace CGame
{
    public readonly struct CameraPoseDelta
    {
        public CameraPoseDelta(Vector3 localPosition, Quaternion localRotation, float weight)
        {
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            Weight = Mathf.Clamp01(weight);
        }

        public Vector3 LocalPosition { get; }
        public Quaternion LocalRotation { get; }
        public float Weight { get; }

        public static CameraPoseDelta None => new CameraPoseDelta(Vector3.zero, Quaternion.identity, 0f);
    }
}
