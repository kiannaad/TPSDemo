using System;
using Unity.Profiling;
using UnityEngine;

namespace CGame
{
    public sealed class AIAlertDecisionRuntimeBehaviour : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("CGame.AI.Decision.Update");
        private AIRuntimeRegistration runtimeRegistration;
        private DecisionProfile profile;
        private AIAlertStateMachine stateMachine;
        private AIUtilitySelector utilitySelector;
        private AIActionExecution currentExecution;
        private AIUtilitySelection lastSelection;
        private Vector3 homePosition;
        private Vector3 lastThreatPosition;
        private float lastThreatConfidence;
        private bool hasThreat;
        private bool hasPreciseThreat;
        private double nextDecisionTime;
        private double lastTickTime;
        private bool isShutdown;

        public AIAlertState State => stateMachine?.State ?? AIAlertState.Patrol;
        public AIActionExecution CurrentExecution => currentExecution;

        public void Initialize(
            AIRuntimeRegistration registration,
            DecisionProfile configuredProfile,
            int randomSeed)
        {
            if (runtimeRegistration != null)
            {
                throw new InvalidOperationException("Decision runtime is already initialized.");
            }

            runtimeRegistration = registration ?? throw new ArgumentNullException(nameof(registration));
            profile = configuredProfile != null && configuredProfile.IsValid
                ? configuredProfile
                : throw new ArgumentException("A valid decision profile is required.", nameof(configuredProfile));
            if (runtimeRegistration.Perception == null)
            {
                throw new InvalidOperationException("Perception must be attached before Decision.");
            }

            double timestamp = Time.timeAsDouble;
            stateMachine = new AIAlertStateMachine(timestamp);
            utilitySelector = new AIUtilitySelector(profile, randomSeed);
            homePosition = runtimeRegistration.Transform.position;
            nextDecisionTime = timestamp;
            lastTickTime = timestamp;
            runtimeRegistration.Health.Damaged += OnDamaged;
            runtimeRegistration.Health.Died += OnDied;
        }

        public void Tick(double timestamp)
        {
            using (UpdateMarker.Auto())
            {
                TickInternal(timestamp);
            }
        }

        private void TickInternal(double timestamp)
        {
            if (isShutdown
                || runtimeRegistration == null
                || !runtimeRegistration.IsActive
                || !runtimeRegistration.IsAlive
                || double.IsNaN(timestamp)
                || double.IsInfinity(timestamp))
            {
                return;
            }

            lastTickTime = timestamp;
            RefreshThreatFacts();
            UpdateState(timestamp);
            if (stateMachine.State == AIAlertState.Combat
                && runtimeRegistration.CoverCombat != null)
            {
                return;
            }

            HandleNavigationFailure(timestamp);

            if (currentExecution != null && currentExecution.IsRunning)
            {
                currentExecution.Advance(timestamp);
                if (!currentExecution.IsRunning)
                {
                    FinishCurrentAction(timestamp);
                }
            }

            if (currentExecution != null
                && currentExecution.IsRunning
                && timestamp < currentExecution.Request.CommitmentUntil)
            {
                ApplyCurrentAction();
                return;
            }

            if (timestamp < nextDecisionTime)
            {
                ApplyCurrentAction();
                return;
            }

            ReselectAction(timestamp);
            nextDecisionTime = timestamp + profile.ReevaluationInterval;
            ApplyCurrentAction();
        }

        public void NotifyTargetDeath(double timestamp)
        {
            ForceInterrupt(timestamp, "target-death");
            hasThreat = false;
            hasPreciseThreat = false;
            if (stateMachine.State != AIAlertState.Patrol)
            {
                stateMachine.TryTransition(AIAlertState.Return, timestamp, "target-death");
            }
        }

        public void NotifyPathFailure(double timestamp)
        {
            if (currentExecution != null && currentExecution.IsRunning)
            {
                currentExecution.Fail("path-failure");
                FinishCurrentAction(timestamp);
            }

            if (stateMachine.State == AIAlertState.Investigate
                || stateMachine.State == AIAlertState.Combat)
            {
                stateMachine.TryTransition(AIAlertState.Search, timestamp, "path-failure");
            }
        }

        public void NotifyMajorDamage(double timestamp)
        {
            ForceInterrupt(timestamp, "major-damage");
            if (stateMachine != null && stateMachine.State == AIAlertState.Combat)
            {
                runtimeRegistration?.CoverCombat?.RequestReposition(timestamp, "major-damage");
            }

            nextDecisionTime = timestamp;
        }

        public AIDecisionDebugSnapshot CreateDebugSnapshot()
        {
            AIActionKind actionKind = currentExecution?.Request.Kind ?? AIActionKind.Hold;
            AIActionStatus actionStatus = currentExecution?.Status ?? AIActionStatus.Completed;
            float commitmentRemaining = currentExecution != null && currentExecution.IsRunning
                ? Mathf.Max(0f, (float)(currentExecution.Request.CommitmentUntil - lastTickTime))
                : 0f;
            return new AIDecisionDebugSnapshot(
                lastTickTime,
                stateMachine?.State ?? AIAlertState.Patrol,
                actionKind,
                actionStatus,
                lastSelection?.Reason ?? stateMachine?.LastReason ?? string.Empty,
                commitmentRemaining,
                lastSelection?.Candidates,
                stateMachine?.CopyHistory());
        }

        public void Shutdown()
        {
            if (isShutdown)
            {
                return;
            }

            isShutdown = true;
            if (runtimeRegistration?.Health != null)
            {
                runtimeRegistration.Health.Damaged -= OnDamaged;
                runtimeRegistration.Health.Died -= OnDied;
            }

            ForceInterrupt(lastTickTime, "shutdown");
            runtimeRegistration?.Navigation?.Cancel();
            runtimeRegistration?.SubmitControlFrame(default);
            utilitySelector?.Clear();
            runtimeRegistration = null;
            profile = null;
            stateMachine = null;
            utilitySelector = null;
            currentExecution = null;
            lastSelection = null;
            enabled = false;
        }

        private void Update()
        {
            Tick(Time.timeAsDouble);
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private void RefreshThreatFacts()
        {
            AIPerceptionMemory memory = runtimeRegistration.Perception?.Memory;
            AIPerceptionMemoryRecord[] records = memory?.CopyRecords()
                ?? Array.Empty<AIPerceptionMemoryRecord>();
            hasThreat = false;
            hasPreciseThreat = false;
            lastThreatConfidence = 0f;
            double newestObservation = double.MinValue;
            for (int i = 0; i < records.Length; i++)
            {
                AIPerceptionMemoryRecord record = records[i];
                bool isPrecise = record.Channel == AIPerceptionChannel.Visual
                    && record.IsPrecise
                    && record.Confidence >= profile.CombatConfidenceThreshold;
                if (!hasThreat
                    || isPrecise && !hasPreciseThreat
                    || isPrecise == hasPreciseThreat && record.ObservedAt > newestObservation)
                {
                    hasThreat = true;
                    hasPreciseThreat = isPrecise;
                    lastThreatPosition = record.LastKnownPosition;
                    lastThreatConfidence = record.Confidence;
                    newestObservation = record.ObservedAt;
                }
            }

            if (!hasPreciseThreat
                && runtimeRegistration.SquadMember != null
                && runtimeRegistration.SquadMember.TryGetSuggestion(lastTickTime, out AISquadSuggestion suggestion))
            {
                float healthNormalized = runtimeRegistration.Health.MaxHealth > 0f
                    ? runtimeRegistration.Health.CurrentHealth / runtimeRegistration.Health.MaxHealth
                    : 0f;
                bool pathReachable = runtimeRegistration.Navigation
                    .EvaluateDestination(suggestion.EstimatedPosition)
                    .HasTraversablePath;
                if (suggestion.ShouldAccept(
                    runtimeRegistration.IsAlive && healthNormalized > 0.15f,
                    pathReachable,
                    profile.CombatConfidenceThreshold * 0.75f))
                {
                    hasThreat = true;
                    hasPreciseThreat = false;
                    lastThreatPosition = suggestion.EstimatedPosition;
                    lastThreatConfidence = suggestion.Confidence;
                }
            }
        }

        private void UpdateState(double timestamp)
        {
            AIAlertState current = stateMachine.State;
            if (hasPreciseThreat)
            {
                if (current != AIAlertState.Combat)
                {
                    TransitionAndInterrupt(AIAlertState.Combat, timestamp, "precise-visual");
                }

                return;
            }

            switch (current)
            {
                case AIAlertState.Patrol:
                    if (hasThreat)
                    {
                        TransitionAndInterrupt(AIAlertState.Investigate, timestamp, "uncertain-stimulus");
                    }
                    break;
                case AIAlertState.Investigate:
                    if (!hasThreat)
                    {
                        TransitionAndInterrupt(AIAlertState.Search, timestamp, "memory-expired");
                    }
                    break;
                case AIAlertState.Combat:
                    TransitionAndInterrupt(AIAlertState.Search, timestamp, "lost-visual");
                    break;
                case AIAlertState.Search:
                    if (timestamp - stateMachine.EnteredAt >= profile.SearchDuration)
                    {
                        TransitionAndInterrupt(AIAlertState.Return, timestamp, "search-finished");
                    }
                    break;
                case AIAlertState.Return:
                    if (hasThreat)
                    {
                        TransitionAndInterrupt(AIAlertState.Investigate, timestamp, "new-uncertain-stimulus");
                    }
                    else if (timestamp - stateMachine.EnteredAt >= profile.ReturnDuration)
                    {
                        TransitionAndInterrupt(AIAlertState.Patrol, timestamp, "returned-home");
                    }
                    break;
            }
        }

        private void TransitionAndInterrupt(AIAlertState next, double timestamp, string reason)
        {
            if (stateMachine.TryTransition(next, timestamp, reason))
            {
                ForceInterrupt(timestamp, $"state-change:{reason}");
                nextDecisionTime = timestamp;
            }
        }

        private void HandleNavigationFailure(double timestamp)
        {
            if (currentExecution == null || !currentExecution.IsRunning)
            {
                return;
            }

            AIActionKind kind = currentExecution.Request.Kind;
            if (kind != AIActionKind.Approach
                && kind != AIActionKind.Retreat
                && kind != AIActionKind.SearchPoint)
            {
                return;
            }

            AIPathFollowState state = runtimeRegistration.Navigation.LastOutput.State;
            if (state == AIPathFollowState.Failed || state == AIPathFollowState.Stuck)
            {
                NotifyPathFailure(timestamp);
            }
        }

        private void ReselectAction(double timestamp)
        {
            if (currentExecution != null && currentExecution.IsRunning)
            {
                if (!currentExecution.Cancel(timestamp, "reevaluate", false))
                {
                    return;
                }

                FinishCurrentAction(timestamp);
            }

            float healthNormalized = runtimeRegistration.Health.MaxHealth > 0f
                ? runtimeRegistration.Health.CurrentHealth / runtimeRegistration.Health.MaxHealth
                : 0f;
            var context = new AIDecisionContext(
                stateMachine.State,
                runtimeRegistration.Transform.position,
                hasThreat,
                hasThreat ? lastThreatPosition : homePosition,
                lastThreatConfidence,
                healthNormalized,
                timestamp);
            lastSelection = utilitySelector.Select(context);
            AIActionKind selectedKind = lastSelection.SelectedKind;
            Vector3 targetPosition = ResolveActionTarget(selectedKind);
            Vector3 aimDirection = targetPosition - runtimeRegistration.Transform.position;
            var request = new AIActionRequest(
                selectedKind,
                targetPosition,
                aimDirection,
                timestamp,
                profile.MinimumCommitment,
                profile.GetMaximumDuration(selectedKind));
            currentExecution = new AIActionExecution(request);
            BeginCurrentAction(timestamp);
        }

        private Vector3 ResolveActionTarget(AIActionKind kind)
        {
            Vector3 currentPosition = runtimeRegistration.Transform.position;
            if (kind == AIActionKind.Retreat && hasThreat)
            {
                Vector3 away = currentPosition - lastThreatPosition;
                away.y = 0f;
                away = away.sqrMagnitude > 0.000001f
                    ? away.normalized
                    : -runtimeRegistration.Transform.forward;
                return currentPosition + away * profile.RetreatDistance;
            }

            if (stateMachine.State == AIAlertState.Return)
            {
                return homePosition;
            }

            return hasThreat ? lastThreatPosition : homePosition;
        }

        private void BeginCurrentAction(double timestamp)
        {
            AIActionKind kind = currentExecution.Request.Kind;
            if (kind == AIActionKind.Approach
                || kind == AIActionKind.Retreat
                || kind == AIActionKind.SearchPoint)
            {
                AINavigationPathResult path = runtimeRegistration.Navigation.SetDestination(
                    currentExecution.Request.TargetPosition);
                if (!path.HasTraversablePath)
                {
                    currentExecution.Fail($"navigation:{path.Status}");
                    FinishCurrentAction(timestamp);
                }
            }
        }

        private void ApplyCurrentAction()
        {
            if (stateMachine.State == AIAlertState.Combat
                && runtimeRegistration.CoverCombat != null)
            {
                return;
            }

            if (currentExecution == null || !currentExecution.IsRunning)
            {
                return;
            }

            AIActionRequest request = currentExecution.Request;
            switch (request.Kind)
            {
                case AIActionKind.Hold:
                    break;
                case AIActionKind.Aim:
                    runtimeRegistration.SubmitControlFrame(new AIControlFrame(
                        Vector3.zero,
                        request.AimDirection,
                        false,
                        false,
                        false));
                    break;
                case AIActionKind.Approach:
                case AIActionKind.Retreat:
                case AIActionKind.SearchPoint:
                    break;
            }
        }

        private void FinishCurrentAction(double timestamp)
        {
            if (currentExecution == null)
            {
                return;
            }

            ReleaseOwnedOutput(currentExecution.Request.Kind);
            if (currentExecution.Request.Kind != AIActionKind.Hold)
            {
                utilitySelector.SetCooldown(
                    currentExecution.Request.Kind,
                    timestamp + profile.ActionCooldown);
            }
        }

        private void ForceInterrupt(double timestamp, string reason)
        {
            if (currentExecution == null)
            {
                return;
            }

            currentExecution.Cancel(timestamp, reason, true);
            ReleaseOwnedOutput(currentExecution.Request.Kind);
        }

        private void ReleaseOwnedOutput(AIActionKind kind)
        {
            if (kind == AIActionKind.Approach
                || kind == AIActionKind.Retreat
                || kind == AIActionKind.SearchPoint)
            {
                runtimeRegistration?.Navigation?.Cancel();
            }

            if (kind == AIActionKind.Aim)
            {
                runtimeRegistration?.SubmitControlFrame(default);
            }
        }

        private void OnDamaged(DamageEvent damageEvent)
        {
            if (runtimeRegistration == null || profile == null)
            {
                return;
            }

            float fraction = runtimeRegistration.Health.MaxHealth > 0f
                ? damageEvent.Amount / runtimeRegistration.Health.MaxHealth
                : 1f;
            if (fraction >= 0.25f)
            {
                NotifyMajorDamage(damageEvent.Timestamp);
            }
        }

        private void OnDied(DamageEvent damageEvent)
        {
            ForceInterrupt(damageEvent.Timestamp, "ai-death");
            enabled = false;
        }
    }
}
