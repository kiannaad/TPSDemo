using UnityEngine;

namespace CGame
{
    public readonly struct CameraPose
    {
        public CameraPose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
    }
}
