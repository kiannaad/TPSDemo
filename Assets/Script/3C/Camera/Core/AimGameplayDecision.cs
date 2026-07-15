using System;

namespace CGame
{
    public sealed class AimGameplayDecision
    {
        public AimGameplayDecision(bool aimHeld, bool isAiming, AimRejectionReason rejectionReason)
        {
            if (!Enum.IsDefined(typeof(AimRejectionReason), rejectionReason))
            {
                throw new ArgumentOutOfRangeException(nameof(rejectionReason));
            }

            if (isAiming && (!aimHeld || rejectionReason != AimRejectionReason.None))
            {
                throw new ArgumentException("An allowed aim decision requires held intent and no rejection reason.");
            }

            if (!isAiming && aimHeld &&
                (rejectionReason == AimRejectionReason.None || rejectionReason == AimRejectionReason.IntentReleased))
            {
                throw new ArgumentException("Rejected held aim intent requires an explicit gameplay rejection reason.");
            }

            if (!aimHeld && (isAiming || rejectionReason != AimRejectionReason.IntentReleased))
            {
                throw new ArgumentException("Released aim intent must produce the IntentReleased result.");
            }

            AimHeld = aimHeld;
            IsAiming = isAiming;
            RejectionReason = rejectionReason;
        }

        public bool AimHeld { get; }
        public bool IsAiming { get; }
        public AimRejectionReason RejectionReason { get; }

        public static AimGameplayDecision Released =>
            new AimGameplayDecision(false, false, AimRejectionReason.IntentReleased);

        public static AimGameplayDecision Allowed =>
            new AimGameplayDecision(true, true, AimRejectionReason.None);

        public static AimGameplayDecision Rejected(AimRejectionReason rejectionReason)
        {
            return new AimGameplayDecision(true, false, rejectionReason);
        }
    }
}
