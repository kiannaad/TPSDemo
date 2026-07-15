using System;
using UnityEngine;
using UnityEngine.AI;

namespace CGame
{
    public sealed class NavMeshNavigationQuery : IAINavigationQuery
    {
        private readonly float sampleDistance;
        private readonly int areaMask;

        public NavMeshNavigationQuery(float sampleDistance = 1f, int areaMask = NavMesh.AllAreas)
        {
            if (float.IsNaN(sampleDistance) || float.IsInfinity(sampleDistance) || sampleDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleDistance));
            }

            this.sampleDistance = sampleDistance;
            this.areaMask = areaMask;
        }

        public AINavigationPathResult CalculatePath(Vector3 start, Vector3 destination)
        {
            if (!IsFinite(start) || !NavMesh.SamplePosition(start, out NavMeshHit sampledStart, sampleDistance, areaMask))
            {
                return AINavigationPathResult.FromStatus(AINavigationPathStatus.StartOutsideNavMesh);
            }

            if (!IsFinite(destination)
                || !NavMesh.SamplePosition(destination, out NavMeshHit sampledDestination, sampleDistance, areaMask))
            {
                return AINavigationPathResult.FromStatus(AINavigationPathStatus.DestinationOutsideNavMesh);
            }

            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(sampledStart.position, sampledDestination.position, areaMask, path))
            {
                return AINavigationPathResult.FromStatus(AINavigationPathStatus.NoPath);
            }

            switch (path.status)
            {
                case NavMeshPathStatus.PathComplete:
                    return new AINavigationPathResult(AINavigationPathStatus.Complete, path.corners);
                case NavMeshPathStatus.PathPartial:
                    return new AINavigationPathResult(AINavigationPathStatus.Partial, path.corners);
                default:
                    return AINavigationPathResult.FromStatus(AINavigationPathStatus.NoPath);
            }
        }

        public AINavigationPathResult Cancel()
        {
            return AINavigationPathResult.FromStatus(AINavigationPathStatus.Cancelled);
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
                && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }
    }
}
