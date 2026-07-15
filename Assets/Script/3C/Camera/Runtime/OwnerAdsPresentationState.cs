using System;
using UnityEngine;

namespace CGame
{
    public sealed class OwnerAdsPresentationState : IAdsPresentationState
    {
        private AimGameplayDecision decision = AimGameplayDecision.Released;

        public float AdsProgress { get; private set; }
        public bool IsAiming => decision.IsAiming;
        public AimRejectionReason RejectionReason => decision.RejectionReason;

        public void ApplyDecision(AimGameplayDecision aimDecision)
        {
            if (aimDecision == null)
            {
                throw new ArgumentNullException(nameof(aimDecision));
            }

            decision = aimDecision;
        }

        public void Advance(float elapseSeconds, float enterDuration, float exitDuration)
        {
            ValidateDuration(elapseSeconds, nameof(elapseSeconds));
            ValidateDuration(enterDuration, nameof(enterDuration));
            ValidateDuration(exitDuration, nameof(exitDuration));

            float targetProgress = decision.IsAiming ? 1f : 0f;
            float duration = decision.IsAiming ? enterDuration : exitDuration;
            if (duration <= 0f)
            {
                AdsProgress = targetProgress;
                return;
            }

            AdsProgress = Mathf.MoveTowards(AdsProgress, targetProgress, elapseSeconds / duration);
        }

        public void Reset()
        {
            decision = AimGameplayDecision.Released;
            AdsProgress = 0f;
        }

        private static void ValidateDuration(float value, string parameterName)
        {
            if (value < 0f || float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
