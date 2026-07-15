using UnityEngine;

namespace CGame
{
    public readonly struct AIPathFollowOutput
    {
        public AIPathFollowOutput(AIPathFollowState state, Vector3 movementDirection, int cornerIndex)
        {
            State = state;
            MovementDirection = movementDirection;
            CornerIndex = cornerIndex;
        }

        public AIPathFollowState State { get; }
        public Vector3 MovementDirection { get; }
        public int CornerIndex { get; }
    }
}
