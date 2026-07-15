using System;
using Unity.Profiling;
using UnityEngine;

namespace CGame
{
    public sealed class AINavigationRuntimeBehaviour : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("CGame.AI.Navigation.Update");
        private AIRuntimeRegistration runtimeRegistration;
        private IAINavigationQuery navigationQuery;
        private AIPathFollower pathFollower;
        private Vector3 destination;
        private bool hasDestination;
        private int replanAttempts;

        public AINavigationPathResult LastPathResult { get; private set; }
        public AIPathFollowOutput LastOutput { get; private set; }
        public bool HasDestination => hasDestination;

        public void Initialize(
            AIRuntimeRegistration registration,
            IAINavigationQuery query = null,
            AIPathFollower follower = null)
        {
            if (runtimeRegistration != null)
            {
                throw new InvalidOperationException("Navigation runtime is already initialized.");
            }

            runtimeRegistration = registration ?? throw new ArgumentNullException(nameof(registration));
            navigationQuery = query ?? new NavMeshNavigationQuery();
            pathFollower = follower ?? new AIPathFollower(
                cornerTolerance: 0.35f,
                progressTimeout: 2f,
                maxPathAge: 10f);
            LastOutput = new AIPathFollowOutput(AIPathFollowState.Idle, Vector3.zero, 0);
        }

        public AINavigationPathResult SetDestination(Vector3 requestedDestination)
        {
            if (runtimeRegistration == null || !runtimeRegistration.IsActive)
            {
                LastPathResult = AINavigationPathResult.FromStatus(AINavigationPathStatus.Cancelled);
                return LastPathResult;
            }

            destination = requestedDestination;
            hasDestination = true;
            replanAttempts = 0;
            return QueryPath();
        }

        public AINavigationPathResult EvaluateDestination(Vector3 requestedDestination)
        {
            if (runtimeRegistration == null || !runtimeRegistration.IsActive || navigationQuery == null)
            {
                return AINavigationPathResult.FromStatus(AINavigationPathStatus.Cancelled);
            }

            return navigationQuery.CalculatePath(runtimeRegistration.Transform.position, requestedDestination);
        }

        public AIPathFollowOutput Advance(float deltaTime)
        {
            using (UpdateMarker.Auto())
            {
                return AdvanceInternal(deltaTime);
            }
        }

        public AINavigationDebugSnapshot CreateDebugSnapshot()
        {
            return new AINavigationDebugSnapshot(
                hasDestination,
                destination,
                LastPathResult.Status,
                LastOutput.State,
                LastOutput.CornerIndex,
                LastPathResult.Corners);
        }

        private AIPathFollowOutput AdvanceInternal(float deltaTime)
        {
            if (runtimeRegistration == null || !runtimeRegistration.IsActive)
            {
                return StopMovement(LastOutput.State);
            }

            if (!hasDestination)
            {
                return LastOutput;
            }

            LastOutput = pathFollower.Advance(runtimeRegistration.Transform.position, deltaTime);
            if (LastOutput.State == AIPathFollowState.Following)
            {
                SubmitMovement(LastOutput.MovementDirection);
                return LastOutput;
            }

            if ((LastOutput.State == AIPathFollowState.NeedsRepath || LastOutput.State == AIPathFollowState.Stuck)
                && replanAttempts < 1)
            {
                replanAttempts++;
                QueryPath();
                return LastOutput;
            }

            if (LastOutput.State == AIPathFollowState.Arrived
                || LastOutput.State == AIPathFollowState.Failed
                || LastOutput.State == AIPathFollowState.Stuck
                || LastOutput.State == AIPathFollowState.Cancelled)
            {
                hasDestination = false;
            }

            return StopMovement(LastOutput.State);
        }

        public AIPathFollowOutput Cancel()
        {
            navigationQuery?.Cancel();
            hasDestination = false;
            LastPathResult = AINavigationPathResult.FromStatus(AINavigationPathStatus.Cancelled);
            LastOutput = pathFollower == null
                ? new AIPathFollowOutput(AIPathFollowState.Cancelled, Vector3.zero, 0)
                : pathFollower.Cancel();
            StopMovement(LastOutput.State);
            return LastOutput;
        }

        public void Shutdown()
        {
            if (runtimeRegistration == null)
            {
                return;
            }

            Cancel();
            runtimeRegistration = null;
            navigationQuery = null;
            pathFollower = null;
            enabled = false;
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private AINavigationPathResult QueryPath()
        {
            LastPathResult = navigationQuery.CalculatePath(runtimeRegistration.Transform.position, destination);
            LastOutput = pathFollower.SetPath(LastPathResult, runtimeRegistration.Transform.position);
            if (LastOutput.State != AIPathFollowState.Following)
            {
                hasDestination = false;
                StopMovement(LastOutput.State);
            }

            return LastPathResult;
        }

        private void SubmitMovement(Vector3 direction)
        {
            runtimeRegistration.SubmitControlFrame(new AIControlFrame(
                direction,
                direction,
                false,
                false,
                false));
        }

        private AIPathFollowOutput StopMovement(AIPathFollowState state)
        {
            runtimeRegistration?.SubmitControlFrame(default);
            LastOutput = new AIPathFollowOutput(state, Vector3.zero, LastOutput.CornerIndex);
            return LastOutput;
        }
    }
}
