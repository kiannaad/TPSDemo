using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace CGame
{
    public sealed class AIPerceptionRuntimeBehaviour : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("CGame.AI.Perception.Update");
        private readonly List<IAIPerceptionTarget> targets = new List<IAIPerceptionTarget>();
        private readonly Queue<AIStimulus> pendingStimuli = new Queue<AIStimulus>();

        private AIRuntimeRegistration runtimeRegistration;
        private PerceptionProfile profile;
        private IAILineOfSightQuery lineOfSightQuery;
        private AIVisualSensor visualSensor;
        private AIPerceptionMemory memory;
        private double nextUpdateTime;
        private double lastUpdateTime;

        public AIPerceptionMemory Memory => memory;
        public int PendingStimulusCount => pendingStimuli.Count;
        public float ViewDistance => profile?.ViewDistance ?? 0f;
        public float HorizontalFieldOfView => profile?.HorizontalFieldOfView ?? 0f;

        public void Initialize(
            AIRuntimeRegistration registration,
            PerceptionProfile configuredProfile,
            IAILineOfSightQuery configuredLineOfSightQuery = null)
        {
            if (runtimeRegistration != null)
            {
                throw new InvalidOperationException("Perception runtime is already initialized.");
            }

            runtimeRegistration = registration ?? throw new ArgumentNullException(nameof(registration));
            profile = configuredProfile != null && configuredProfile.IsValid
                ? configuredProfile
                : throw new ArgumentException("A valid perception profile is required.", nameof(configuredProfile));
            lineOfSightQuery = configuredLineOfSightQuery ?? new PhysicsAILineOfSightQuery();
            visualSensor = new AIVisualSensor(profile);
            memory = new AIPerceptionMemory(profile);
            double phase = (uint)runtimeRegistration.RuntimeId.Value.GetHashCode() % 1000u / 1000d;
            nextUpdateTime = Time.timeAsDouble + phase * profile.PerceptionInterval;
            lastUpdateTime = Time.timeAsDouble;
            if (runtimeRegistration.Health != null)
            {
                runtimeRegistration.Health.Damaged += OnDamaged;
            }
        }

        public bool RegisterTarget(IAIPerceptionTarget target)
        {
            if (target == null || string.IsNullOrWhiteSpace(target.EntityId) || targets.Contains(target))
            {
                return false;
            }

            targets.Add(target);
            return true;
        }

        public bool UnregisterTarget(IAIPerceptionTarget target)
        {
            if (target == null || !targets.Remove(target))
            {
                return false;
            }

            visualSensor?.ForgetTarget(target.EntityId);
            return true;
        }

        public void PublishSound(
            string sourceEntityId,
            Vector3 position,
            double timestamp,
            float uncertaintyRadius = -1f)
        {
            if (profile == null)
            {
                return;
            }

            float resolvedUncertainty = uncertaintyRadius >= 0f
                ? uncertaintyRadius
                : profile.DefaultSoundUncertainty;
            pendingStimuli.Enqueue(AIStimulus.CreateSound(
                sourceEntityId,
                position,
                timestamp,
                resolvedUncertainty,
                profile.SoundConfidence));
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
            if (runtimeRegistration == null
                || !runtimeRegistration.IsActive
                || timestamp + 0.000001d < nextUpdateTime)
            {
                return;
            }

            while (pendingStimuli.Count > 0)
            {
                memory.Observe(pendingStimuli.Dequeue());
            }

            Vector3 observerPosition = runtimeRegistration.Transform.position + Vector3.up * 1.6f;
            Vector3 observerForward = runtimeRegistration.Transform.forward;
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                IAIPerceptionTarget target = targets[i];
                if (target == null || !target.IsActive)
                {
                    if (target != null)
                    {
                        visualSensor.ForgetTarget(target.EntityId);
                    }

                    targets.RemoveAt(i);
                    continue;
                }

                bool hasLineOfSight = lineOfSightQuery.HasLineOfSight(
                    observerPosition,
                    runtimeRegistration.Transform,
                    target,
                    profile.OcclusionMask);
                if (visualSensor.TryObserve(
                    target.EntityId,
                    observerPosition,
                    observerForward,
                    target.Position,
                    hasLineOfSight,
                    timestamp,
                    out AIStimulus stimulus))
                {
                    memory.Observe(stimulus);
                }
            }

            memory.Advance(timestamp);
            runtimeRegistration.SquadMember?.Tick(timestamp);
            lastUpdateTime = timestamp;
            nextUpdateTime = timestamp + profile.PerceptionInterval;
        }

        public AIPerceptionDebugSnapshot CreateDebugSnapshot()
        {
            return new AIPerceptionDebugSnapshot(
                lastUpdateTime,
                pendingStimuli.Count,
                memory?.CopyRecords());
        }

        public void Shutdown()
        {
            if (runtimeRegistration == null)
            {
                return;
            }

            if (runtimeRegistration.Health != null)
            {
                runtimeRegistration.Health.Damaged -= OnDamaged;
            }

            targets.Clear();
            pendingStimuli.Clear();
            visualSensor?.Clear();
            memory?.Clear();
            runtimeRegistration = null;
            profile = null;
            lineOfSightQuery = null;
            visualSensor = null;
            memory = null;
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

        private void OnDamaged(DamageEvent damageEvent)
        {
            if (profile == null)
            {
                return;
            }

            pendingStimuli.Enqueue(AIStimulus.CreateDamage(
                damageEvent.SourceEntityId,
                damageEvent.HitPoint,
                damageEvent.Direction,
                damageEvent.Timestamp,
                profile.DamageConfidence));
        }
    }
}
