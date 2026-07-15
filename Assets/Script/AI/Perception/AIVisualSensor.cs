using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGame
{
    public sealed class AIVisualSensor
    {
        private readonly PerceptionProfile profile;
        private readonly Dictionary<string, double> visibleSince = new Dictionary<string, double>();

        public AIVisualSensor(PerceptionProfile profile)
        {
            this.profile = profile != null && profile.IsValid
                ? profile
                : throw new ArgumentException("A valid perception profile is required.", nameof(profile));
        }

        public bool TryObserve(
            string targetEntityId,
            Vector3 observerPosition,
            Vector3 observerForward,
            Vector3 targetPosition,
            bool hasLineOfSight,
            double timestamp,
            out AIStimulus stimulus)
        {
            stimulus = default;
            if (string.IsNullOrWhiteSpace(targetEntityId)
                || double.IsNaN(timestamp)
                || double.IsInfinity(timestamp))
            {
                return false;
            }

            Vector3 toTarget = targetPosition - observerPosition;
            toTarget.y = 0f;
            observerForward.y = 0f;
            float distance = toTarget.magnitude;
            bool insideRange = distance <= profile.ViewDistance;
            bool hasDirections = distance > 0.000001f && observerForward.sqrMagnitude > 0.000001f;
            bool insideFieldOfView = hasDirections
                && Vector3.Angle(observerForward, toTarget) <= profile.HorizontalFieldOfView * 0.5f + 0.0001f;
            if (!hasLineOfSight || !insideRange || !insideFieldOfView)
            {
                visibleSince.Remove(targetEntityId);
                return false;
            }

            if (!visibleSince.TryGetValue(targetEntityId, out double firstSeen))
            {
                firstSeen = timestamp;
                visibleSince.Add(targetEntityId, firstSeen);
            }

            if (timestamp - firstSeen + 0.000001d < profile.RecognitionDuration)
            {
                return false;
            }

            stimulus = AIStimulus.CreateVisual(
                targetEntityId,
                targetPosition,
                timestamp,
                profile.VisualConfidence);
            return true;
        }

        public void ForgetTarget(string targetEntityId)
        {
            if (!string.IsNullOrWhiteSpace(targetEntityId))
            {
                visibleSince.Remove(targetEntityId);
            }
        }

        public void Clear()
        {
            visibleSince.Clear();
        }
    }
}
