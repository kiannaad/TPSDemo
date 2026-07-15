using System;
using UnityEngine;

namespace CGame
{
    public readonly struct AINavigationPathResult
    {
        public AINavigationPathResult(AINavigationPathStatus status, Vector3[] corners)
        {
            Status = status;
            Corners = corners == null ? Array.Empty<Vector3>() : (Vector3[])corners.Clone();
        }

        public AINavigationPathStatus Status { get; }
        public Vector3[] Corners { get; }
        public bool HasTraversablePath => (Status == AINavigationPathStatus.Complete
                || Status == AINavigationPathStatus.Partial)
            && Corners != null
            && Corners.Length >= 2;

        public static AINavigationPathResult FromStatus(AINavigationPathStatus status)
        {
            return new AINavigationPathResult(status, Array.Empty<Vector3>());
        }
    }
}
