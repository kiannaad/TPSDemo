using UnityEngine;

namespace CGame
{
    public interface IFirstPersonCameraTarget
    {
        Vector3 Position { get; }
        Quaternion Rotation { get; }
        bool IsValid { get; }
    }
}
