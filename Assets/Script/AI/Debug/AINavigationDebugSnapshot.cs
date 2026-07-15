using System;
using UnityEngine;

namespace CGame
{
    public sealed class AINavigationDebugSnapshot
    {
        private readonly Vector3[] corners;

        public AINavigationDebugSnapshot(
            bool hasDestination,
            Vector3 destination,
            AINavigationPathStatus pathStatus,
            AIPathFollowState followState,
            int cornerIndex,
            Vector3[] pathCorners)
        {
            HasDestination = hasDestination;
            Destination = destination;
            PathStatus = pathStatus;
            FollowState = followState;
            CornerIndex = Math.Max(0, cornerIndex);
            corners = pathCorners == null ? Array.Empty<Vector3>() : (Vector3[])pathCorners.Clone();
        }

        public bool HasDestination { get; }
        public Vector3 Destination { get; }
        public AINavigationPathStatus PathStatus { get; }
        public AIPathFollowState FollowState { get; }
        public int CornerIndex { get; }
        public Vector3[] Corners => (Vector3[])corners.Clone();
    }
}
