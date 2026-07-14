using UnityEngine;

namespace CGame
{
    public interface IAIPerceptionTarget
    {
        string EntityId { get; }
        Vector3 Position { get; }
        Transform Transform { get; }
        bool IsActive { get; }
    }
}
