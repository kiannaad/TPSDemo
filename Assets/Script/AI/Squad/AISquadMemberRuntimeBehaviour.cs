using System;
using UnityEngine;

namespace CGame
{
    public sealed class AISquadMemberRuntimeBehaviour : MonoBehaviour
    {
        private const float ReportUncertainty = 2.5f;
        private const float ReportConfidenceScale = 0.75f;
        private const double ReportLifetime = 4d;

        private AIRuntimeRegistration runtimeRegistration;
        private AISquadContext context;
        private AISquadLease shooterLease;
        private AISquadLease repositionLease;
        private double lastPublishedObservation = double.NegativeInfinity;
        private bool isShutdown;

        public AISquadContext Context => context;
        public string MemberId => runtimeRegistration?.RuntimeId.Value;

        public void Initialize(AIRuntimeRegistration registration, AISquadContext squadContext)
        {
            if (runtimeRegistration != null)
            {
                throw new InvalidOperationException("Squad member runtime is already initialized.");
            }

            runtimeRegistration = registration ?? throw new ArgumentNullException(nameof(registration));
            context = squadContext ?? throw new ArgumentNullException(nameof(squadContext));
            if (runtimeRegistration.Perception == null)
            {
                throw new InvalidOperationException("Perception must be attached before Squad Member.");
            }

            runtimeRegistration.Health.Died += OnDied;
        }

        public void Tick(double timestamp)
        {
            if (isShutdown
                || runtimeRegistration == null
                || !runtimeRegistration.IsActive
                || !runtimeRegistration.IsAlive)
            {
                return;
            }

            context.Advance(timestamp);
            AIPerceptionMemoryRecord[] records = runtimeRegistration.Perception.Memory?.CopyRecords()
                ?? Array.Empty<AIPerceptionMemoryRecord>();
            AIPerceptionMemoryRecord newest = default;
            bool found = false;
            for (int i = 0; i < records.Length; i++)
            {
                AIPerceptionMemoryRecord record = records[i];
                if (record.Channel == AIPerceptionChannel.Visual
                    && record.IsPrecise
                    && record.ObservedAt > lastPublishedObservation
                    && (!found || record.ObservedAt > newest.ObservedAt))
                {
                    newest = record;
                    found = true;
                }
            }

            if (!found)
            {
                return;
            }

            int hash = runtimeRegistration.RuntimeId.Value.GetHashCode();
            float angle = (uint)hash % 360u;
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * ReportUncertainty;
            double delay = 0.3d + (uint)(hash >> 8) % 5u * 0.05d;
            context.PublishReport(new AISquadReport(
                runtimeRegistration.RuntimeId.Value,
                newest.SourceEntityId,
                newest.LastKnownPosition + offset,
                newest.ObservedAt,
                newest.ObservedAt + delay,
                newest.ObservedAt + ReportLifetime,
                newest.Confidence * ReportConfidenceScale,
                ReportUncertainty));
            lastPublishedObservation = newest.ObservedAt;
        }

        public bool TryGetSuggestion(double timestamp, out AISquadSuggestion suggestion)
        {
            if (context == null)
            {
                suggestion = default;
                return false;
            }

            return context.TryGetLatestSuggestion(MemberId, timestamp, out suggestion);
        }

        public bool TryAcquireShooter(double timestamp, double duration)
        {
            if (shooterLease != null && shooterLease.IsActive)
            {
                return true;
            }

            return context != null
                && context.TryAcquire(
                    AISquadResourceKind.Shooter,
                    "primary",
                    MemberId,
                    timestamp,
                    duration,
                    out shooterLease);
        }

        public bool TryAcquireReposition(double timestamp, double duration)
        {
            if (repositionLease != null && repositionLease.IsActive)
            {
                return true;
            }

            return context != null
                && context.TryAcquire(
                    AISquadResourceKind.Reposition,
                    "primary",
                    MemberId,
                    timestamp,
                    duration,
                    out repositionLease);
        }

        public void ReleaseShooter()
        {
            shooterLease?.Release();
            shooterLease = null;
        }

        public void ReleaseReposition()
        {
            repositionLease?.Release();
            repositionLease = null;
        }

        public AISquadDebugSnapshot CreateDebugSnapshot(double timestamp)
        {
            return context?.CreateDebugSnapshot(timestamp, MemberId);
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
                runtimeRegistration.Health.Died -= OnDied;
            }

            ReleaseShooter();
            ReleaseReposition();
            context?.ReleaseOwner(MemberId);
            runtimeRegistration = null;
            context = null;
            enabled = false;
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private void OnDied(DamageEvent damageEvent)
        {
            Shutdown();
        }
    }
}
