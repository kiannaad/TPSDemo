using UnityEngine;

namespace CGame.Animation
{
    public static class AnimationDebugNotifyLog
    {
        public static int InstantCount { get; private set; }
        public static int DurationBeginCount { get; private set; }
        public static int DurationTickCount { get; private set; }
        public static int DurationEndCount { get; private set; }
        public static AnimationNotifyEndReason LastEndReason { get; private set; }

        public static void Reset()
        {
            InstantCount = 0;
            DurationBeginCount = 0;
            DurationTickCount = 0;
            DurationEndCount = 0;
            LastEndReason = AnimationNotifyEndReason.Interrupted;
        }

        public static void RecordInstant(AnimationEventContext context)
        {
            InstantCount++;
            Debug.Log($"AnimationDebugNotify Instant {context.EventTag} time={context.NormalizedTime:0.###}");
        }

        public static void RecordDurationBegin(AnimationEventContext context)
        {
            DurationBeginCount++;
            Debug.Log($"AnimationDebugNotify DurationBegin {context.EventTag} time={context.NormalizedTime:0.###}");
        }

        public static void RecordDurationTick(AnimationEventContext context)
        {
            DurationTickCount++;
            Debug.Log($"AnimationDebugNotify DurationTick {context.EventTag} time={context.NormalizedTime:0.###}");
        }

        public static void RecordDurationEnd(AnimationEventContext context, AnimationNotifyEndReason reason)
        {
            DurationEndCount++;
            LastEndReason = reason;
            Debug.Log($"AnimationDebugNotify DurationEnd {context.EventTag} reason={reason}");
        }
    }

    public class AnimationDebugInstantNotify : AnimationInstantNotify
    {
        public override void OnNotify(AnimationEventContext context)
        {
            AnimationDebugNotifyLog.RecordInstant(context);
        }
    }

    public class AnimationDebugDurationNotify : AnimationDurationNotify
    {
        public override void OnBegin(AnimationEventContext context)
        {
            AnimationDebugNotifyLog.RecordDurationBegin(context);
        }

        public override void OnTick(AnimationEventContext context)
        {
            AnimationDebugNotifyLog.RecordDurationTick(context);
        }

        public override void OnEnd(AnimationEventContext context, AnimationNotifyEndReason reason)
        {
            AnimationDebugNotifyLog.RecordDurationEnd(context, reason);
        }
    }
}
