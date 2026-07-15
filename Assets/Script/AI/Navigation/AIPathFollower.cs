using System;
using UnityEngine;

namespace CGame
{
    public sealed class AIPathFollower
    {
        private readonly float cornerTolerance;
        private readonly float progressTimeout;
        private readonly float minimumProgress;
        private readonly float maxPathAge;

        private Vector3[] corners = Array.Empty<Vector3>();
        private int cornerIndex;
        private bool isPartial;
        private float bestCornerDistance;
        private float noProgressTime;
        private float pathAge;
        private AIPathFollowState state = AIPathFollowState.Idle;

        public AIPathFollower(
            float cornerTolerance = 0.2f,
            float progressTimeout = 1f,
            float minimumProgress = 0.05f,
            float maxPathAge = 5f)
        {
            if (cornerTolerance <= 0f || progressTimeout <= 0f || minimumProgress < 0f || maxPathAge <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cornerTolerance));
            }

            this.cornerTolerance = cornerTolerance;
            this.progressTimeout = progressTimeout;
            this.minimumProgress = minimumProgress;
            this.maxPathAge = maxPathAge;
        }

        public AIPathFollowState State => state;

        public AIPathFollowOutput SetPath(AINavigationPathResult path, Vector3 currentPosition)
        {
            ResetTracking();
            if (!path.HasTraversablePath)
            {
                state = path.Status == AINavigationPathStatus.Cancelled
                    ? AIPathFollowState.Cancelled
                    : AIPathFollowState.Failed;
                return CurrentOutput(Vector3.zero);
            }

            corners = (Vector3[])path.Corners.Clone();
            cornerIndex = 1;
            isPartial = path.Status == AINavigationPathStatus.Partial;
            state = AIPathFollowState.Following;
            bestCornerDistance = PlanarDistance(currentPosition, corners[cornerIndex]);
            return FollowingOutput(currentPosition);
        }

        public AIPathFollowOutput Advance(Vector3 currentPosition, float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (state != AIPathFollowState.Following)
            {
                return CurrentOutput(Vector3.zero);
            }

            pathAge += deltaTime;
            if (pathAge >= maxPathAge)
            {
                state = AIPathFollowState.NeedsRepath;
                return CurrentOutput(Vector3.zero);
            }

            float distance = PlanarDistance(currentPosition, corners[cornerIndex]);
            while (distance <= cornerTolerance)
            {
                cornerIndex++;
                if (cornerIndex >= corners.Length)
                {
                    state = isPartial ? AIPathFollowState.NeedsRepath : AIPathFollowState.Arrived;
                    return CurrentOutput(Vector3.zero);
                }

                distance = PlanarDistance(currentPosition, corners[cornerIndex]);
                bestCornerDistance = distance;
                noProgressTime = 0f;
            }

            if (bestCornerDistance - distance >= minimumProgress)
            {
                bestCornerDistance = distance;
                noProgressTime = 0f;
            }
            else
            {
                noProgressTime += deltaTime;
                if (noProgressTime >= progressTimeout)
                {
                    state = AIPathFollowState.Stuck;
                    return CurrentOutput(Vector3.zero);
                }
            }

            return FollowingOutput(currentPosition);
        }

        public AIPathFollowOutput Cancel()
        {
            ResetTracking();
            state = AIPathFollowState.Cancelled;
            return CurrentOutput(Vector3.zero);
        }

        private AIPathFollowOutput FollowingOutput(Vector3 currentPosition)
        {
            Vector3 direction = corners[cornerIndex] - currentPosition;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector3.zero;
            return CurrentOutput(direction);
        }

        private AIPathFollowOutput CurrentOutput(Vector3 movementDirection)
        {
            return new AIPathFollowOutput(state, movementDirection, cornerIndex);
        }

        private void ResetTracking()
        {
            corners = Array.Empty<Vector3>();
            cornerIndex = 0;
            isPartial = false;
            bestCornerDistance = 0f;
            noProgressTime = 0f;
            pathAge = 0f;
        }

        private static float PlanarDistance(Vector3 left, Vector3 right)
        {
            left.y = 0f;
            right.y = 0f;
            return Vector3.Distance(left, right);
        }
    }
}
