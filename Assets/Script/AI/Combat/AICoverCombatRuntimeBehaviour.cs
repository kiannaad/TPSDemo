using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

namespace CGame
{
    public sealed class AICoverCombatRuntimeBehaviour : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("CGame.AI.CoverCombat.Update");
        private AIRuntimeRegistration runtimeRegistration;
        private CombatProfile profile;
        private CoverScorer scorer;
        private PhysicsCoverQuery coverQuery;
        private CoverReservation reservation;
        private CoverSlotBehaviour reservedSlot;
        private CoverCandidateScore[] lastCandidates = Array.Empty<CoverCandidateScore>();
        private AICombatActionState action;
        private CoverStance stance;
        private Vector3 threatPosition;
        private double actionEnteredAt;
        private double lastTickTime;
        private double lastCoverSelectionTime;
        private double pressureAt;
        private int burstStartShots;
        private string reason = string.Empty;
        private bool hasPreciseThreat;
        private bool isShutdown;

        public AICombatActionState Action => action;
        public CoverStance Stance => stance;
        public CoverReservation Reservation => reservation;
        public Vector3 ThreatPosition => threatPosition;

        public void Initialize(AIRuntimeRegistration registration, CombatProfile configuredProfile)
        {
            if (runtimeRegistration != null)
            {
                throw new InvalidOperationException("Cover combat runtime is already initialized.");
            }

            runtimeRegistration = registration ?? throw new ArgumentNullException(nameof(registration));
            profile = configuredProfile != null && configuredProfile.IsValid
                ? configuredProfile
                : throw new ArgumentException("A valid combat profile is required.", nameof(configuredProfile));
            if (runtimeRegistration.Navigation == null
                || runtimeRegistration.Perception == null
                || runtimeRegistration.Decision == null
                || runtimeRegistration.WeaponRuntime == null)
            {
                throw new InvalidOperationException(
                    "Navigation, Perception, Decision, and Weapon must be attached before Cover Combat.");
            }

            scorer = new CoverScorer(profile);
            coverQuery = new PhysicsCoverQuery(~0);
            pressureAt = double.NegativeInfinity;
            runtimeRegistration.Health.Damaged += OnDamaged;
            runtimeRegistration.Health.Died += OnDied;
            lastTickTime = Time.timeAsDouble;
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
            RefreshThreat();
            if (runtimeRegistration.Decision.State != AIAlertState.Combat || !hasPreciseThreat)
            {
                StopCombat("not-in-combat");
                return;
            }

            switch (action)
            {
                case AICombatActionState.Idle:
                    SelectPositioningAction(timestamp, false);
                    break;
                case AICombatActionState.MoveToCover:
                case AICombatActionState.Approach:
                case AICombatActionState.Retreat:
                    TickMovement(timestamp);
                    break;
                case AICombatActionState.Aim:
                    TickAim(timestamp);
                    break;
                case AICombatActionState.FireBurst:
                    TickFireBurst(timestamp);
                    break;
                case AICombatActionState.Pause:
                    TickPause(timestamp);
                    break;
                case AICombatActionState.Reposition:
                    SelectPositioningAction(timestamp, true);
                    break;
            }
        }

        public void RequestReposition(double timestamp, string requestReason)
        {
            if (runtimeRegistration == null || isShutdown)
            {
                return;
            }

            if (runtimeRegistration.SquadMember != null
                && !runtimeRegistration.SquadMember.TryAcquireReposition(timestamp, 1.5d))
            {
                SubmitAim(1f, false, false);
                EnterAction(AICombatActionState.Pause, timestamp, "reposition-slot-busy");
                return;
            }

            ReleaseReservation();
            runtimeRegistration.Navigation.Cancel();
            runtimeRegistration.SubmitControlFrame(default);
            EnterAction(
                AICombatActionState.Reposition,
                timestamp,
                string.IsNullOrWhiteSpace(requestReason) ? "reposition" : requestReason);
        }

        public void NotifyPathFailure(double timestamp)
        {
            if (action != AICombatActionState.MoveToCover
                && action != AICombatActionState.Approach
                && action != AICombatActionState.Retreat)
            {
                return;
            }

            RequestReposition(timestamp, "path-failure");
        }

        public AICoverCombatDebugSnapshot CreateDebugSnapshot()
        {
            float aimProgress = action == AICombatActionState.Aim
                ? Mathf.Clamp01((float)((lastTickTime - actionEnteredAt) / profile.AimConvergenceDuration))
                : action == AICombatActionState.FireBurst ? 1f : 0f;
            int fired = runtimeRegistration?.WeaponRuntime?.Weapon == null
                ? 0
                : runtimeRegistration.WeaponRuntime.Weapon.ShotsFired - burstStartShots;
            return new AICoverCombatDebugSnapshot(
                lastTickTime,
                action,
                stance,
                reservation?.SlotId,
                aimProgress,
                Mathf.Max(0, profile.BurstLength - fired),
                reason,
                lastCandidates);
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

            ReleaseReservation();
            runtimeRegistration?.Navigation?.Cancel();
            runtimeRegistration?.SubmitControlFrame(default);
            runtimeRegistration = null;
            profile = null;
            scorer = null;
            coverQuery = null;
            reservedSlot = null;
            lastCandidates = Array.Empty<CoverCandidateScore>();
            action = AICombatActionState.Idle;
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

        private void RefreshThreat()
        {
            AIPerceptionMemoryRecord[] records = runtimeRegistration.Perception.Memory?.CopyRecords()
                ?? Array.Empty<AIPerceptionMemoryRecord>();
            hasPreciseThreat = false;
            double newest = double.MinValue;
            for (int i = 0; i < records.Length; i++)
            {
                AIPerceptionMemoryRecord record = records[i];
                if (record.Channel == AIPerceptionChannel.Visual
                    && record.IsPrecise
                    && record.ObservedAt > newest)
                {
                    newest = record.ObservedAt;
                    threatPosition = record.LastKnownPosition;
                    hasPreciseThreat = true;
                }
            }
        }

        private void SelectPositioningAction(double timestamp, bool excludeCurrentSlot)
        {
            string previousSlotId = excludeCurrentSlot ? reservedSlot?.SlotId : null;
            ReleaseReservation();
            CoverSlotBehaviour[] slots = CoverSlotBehaviour.CopyActiveSlots();
            var evaluated = new List<(CoverSlotBehaviour Slot, CoverCandidateScore Score)>();
            for (int i = 0; i < slots.Length; i++)
            {
                CoverSlotBehaviour slot = slots[i];
                AINavigationPathResult path = runtimeRegistration.Navigation.EvaluateDestination(slot.Position);
                bool occupied = CoverReservationService.Shared.IsReservedByOther(
                    slot.SlotId,
                    runtimeRegistration.RuntimeId.Value);
                var context = new CoverEvaluationContext(
                    path.HasTraversablePath,
                    coverQuery.IsThreatOccluded(slot, threatPosition, runtimeRegistration.Transform),
                    coverQuery.HasLineOfFire(slot, threatPosition, runtimeRegistration.Transform),
                    Vector3.Distance(runtimeRegistration.Transform.position, slot.Position),
                    Vector3.Distance(slot.Position, threatPosition),
                    slot.Exposure,
                    path.Status == AINavigationPathStatus.Partial ? 1f : 0f,
                    slot.Stance,
                    occupied);
                CoverCandidateScore score = scorer.Evaluate(slot.SlotId, context);
                evaluated.Add((slot, score));
            }

            lastCandidates = evaluated
                .Select(entry => entry.Score)
                .OrderByDescending(score => score.Score)
                .ToArray();
            var selected = evaluated
                .Where(entry => entry.Score.IsViable
                    && (!excludeCurrentSlot || !string.Equals(
                        entry.Slot.SlotId,
                        previousSlotId,
                        StringComparison.Ordinal)))
                .OrderByDescending(entry => entry.Score.Score)
                .FirstOrDefault();
            if (selected.Slot != null
                && CoverReservationService.Shared.TryReserve(
                    selected.Slot.SlotId,
                    runtimeRegistration.RuntimeId.Value,
                    out CoverReservation acquired))
            {
                reservation = acquired;
                reservedSlot = selected.Slot;
                stance = selected.Slot.Stance;
                lastCoverSelectionTime = timestamp;
                AINavigationPathResult path = runtimeRegistration.Navigation.SetDestination(selected.Slot.Position);
                if (path.HasTraversablePath)
                {
                    EnterAction(AICombatActionState.MoveToCover, timestamp, "best-cover");
                    return;
                }

                ReleaseReservation();
            }

            SelectRangeAction(timestamp);
        }

        private void SelectRangeAction(double timestamp)
        {
            Vector3 flatDelta = threatPosition - runtimeRegistration.Transform.position;
            flatDelta.y = 0f;
            float distance = flatDelta.magnitude;
            float health = runtimeRegistration.Health.MaxHealth > 0f
                ? runtimeRegistration.Health.CurrentHealth / runtimeRegistration.Health.MaxHealth
                : 0f;
            if (health <= profile.LowHealthThreshold
                || distance < profile.PreferredDistance - profile.DistanceTolerance)
            {
                Vector3 away = flatDelta.sqrMagnitude > 0.000001f
                    ? -flatDelta.normalized
                    : -runtimeRegistration.Transform.forward;
                BeginRangeMovement(
                    AICombatActionState.Retreat,
                    runtimeRegistration.Transform.position + away * profile.PreferredDistance,
                    timestamp,
                    health <= profile.LowHealthThreshold ? "low-health" : "target-too-close");
                return;
            }

            if (distance > profile.PreferredDistance + profile.DistanceTolerance)
            {
                Vector3 destination = threatPosition - flatDelta.normalized * profile.PreferredDistance;
                destination.y = runtimeRegistration.Transform.position.y;
                BeginRangeMovement(AICombatActionState.Approach, destination, timestamp, "target-too-far");
                return;
            }

            EnterAction(AICombatActionState.Aim, timestamp, "range-ready");
        }

        private void BeginRangeMovement(
            AICombatActionState movementAction,
            Vector3 destination,
            double timestamp,
            string actionReason)
        {
            AINavigationPathResult path = runtimeRegistration.Navigation.SetDestination(destination);
            if (path.HasTraversablePath)
            {
                EnterAction(movementAction, timestamp, actionReason);
            }
            else
            {
                EnterAction(AICombatActionState.Aim, timestamp, $"navigation:{path.Status}");
            }
        }

        private void TickMovement(double timestamp)
        {
            AIPathFollowState state = runtimeRegistration.Navigation.LastOutput.State;
            if (state == AIPathFollowState.Arrived)
            {
                EnterAction(AICombatActionState.Aim, timestamp, "position-reached");
                TickAim(timestamp);
                return;
            }

            if (state == AIPathFollowState.Failed
                || state == AIPathFollowState.Stuck
                || state == AIPathFollowState.Cancelled)
            {
                NotifyPathFailure(timestamp);
            }
        }

        private void TickAim(double timestamp)
        {
            float progress = Mathf.Clamp01(
                (float)((timestamp - actionEnteredAt) / profile.AimConvergenceDuration));
            SubmitAim(progress, false, false);
            if (progress >= 1f)
            {
                if (runtimeRegistration.SquadMember != null
                    && !runtimeRegistration.SquadMember.TryAcquireShooter(
                        timestamp,
                        profile.BurstInterval + 0.5d))
                {
                    reason = "shooter-slot-busy";
                    return;
                }

                burstStartShots = runtimeRegistration.WeaponRuntime.Weapon.ShotsFired;
                EnterAction(AICombatActionState.FireBurst, timestamp, "aim-converged");
                SubmitAim(1f, true, false);
            }
        }

        private void TickFireBurst(double timestamp)
        {
            WeaponComponent weapon = runtimeRegistration.WeaponRuntime.Weapon;
            int fired = weapon.ShotsFired - burstStartShots;
            if (weapon.AmmoInMagazine <= 0)
            {
                runtimeRegistration.SquadMember?.ReleaseShooter();
                SubmitAim(1f, false, true);
                EnterAction(AICombatActionState.Pause, timestamp, "reload");
                return;
            }

            if (fired >= profile.BurstLength)
            {
                runtimeRegistration.SquadMember?.ReleaseShooter();
                SubmitAim(1f, false, false);
                EnterAction(AICombatActionState.Pause, timestamp, "burst-complete");
                return;
            }

            SubmitAim(1f, true, false);
        }

        private void TickPause(double timestamp)
        {
            SubmitAim(1f, false, runtimeRegistration.WeaponRuntime.Weapon.AmmoInMagazine <= 0);
            if (timestamp - actionEnteredAt < profile.BurstInterval)
            {
                return;
            }

            if (reservation != null
                && timestamp - lastCoverSelectionTime >= profile.RepositionInterval)
            {
                RequestReposition(timestamp, "reposition-interval");
                return;
            }

            EnterAction(AICombatActionState.Aim, timestamp, "burst-pause-finished");
            runtimeRegistration.SquadMember?.ReleaseReposition();
        }

        private void SubmitAim(float convergence, bool fire, bool reload)
        {
            Vector3 aimDirection = threatPosition - runtimeRegistration.Muzzle.position;
            if (aimDirection.sqrMagnitude <= 0.000001f)
            {
                aimDirection = runtimeRegistration.Transform.forward;
            }

            float pressure = Mathf.Clamp01(1f - (float)(lastTickTime - pressureAt) / 2f);
            float error = AICombatAimModel.CalculateErrorDegrees(
                profile,
                convergence,
                action == AICombatActionState.MoveToCover
                    || action == AICombatActionState.Approach
                    || action == AICombatActionState.Retreat,
                pressure);
            float sign = (runtimeRegistration.RuntimeId.Value.GetHashCode() & 1) == 0 ? 1f : -1f;
            aimDirection = Quaternion.AngleAxis(error * sign, Vector3.up) * aimDirection.normalized;
            runtimeRegistration.SubmitControlFrame(new AIControlFrame(
                Vector3.zero,
                aimDirection,
                false,
                fire,
                reload));
        }

        private void EnterAction(AICombatActionState next, double timestamp, string actionReason)
        {
            action = next;
            actionEnteredAt = timestamp;
            reason = actionReason ?? string.Empty;
        }

        private void StopCombat(string stopReason)
        {
            if (action == AICombatActionState.Idle && reservation == null)
            {
                return;
            }

            ReleaseReservation();
            runtimeRegistration.SquadMember?.ReleaseShooter();
            runtimeRegistration.SquadMember?.ReleaseReposition();
            runtimeRegistration.Navigation.Cancel();
            runtimeRegistration.SubmitControlFrame(default);
            EnterAction(AICombatActionState.Idle, lastTickTime, stopReason);
        }

        private void ReleaseReservation()
        {
            reservation?.Release();
            reservation = null;
            reservedSlot = null;
            stance = CoverStance.Standing;
        }

        private void OnDamaged(DamageEvent damageEvent)
        {
            pressureAt = damageEvent.Timestamp;
            if (runtimeRegistration != null
                && runtimeRegistration.Health.MaxHealth > 0f
                && runtimeRegistration.Health.CurrentHealth / runtimeRegistration.Health.MaxHealth
                    <= profile.LowHealthThreshold)
            {
                RequestReposition(damageEvent.Timestamp, "low-health-hit");
            }
        }

        private void OnDied(DamageEvent damageEvent)
        {
            ReleaseReservation();
            runtimeRegistration.SquadMember?.ReleaseShooter();
            runtimeRegistration.SquadMember?.ReleaseReposition();
            runtimeRegistration?.Navigation?.Cancel();
            runtimeRegistration?.SubmitControlFrame(default);
            action = AICombatActionState.Idle;
            enabled = false;
        }
    }
}
