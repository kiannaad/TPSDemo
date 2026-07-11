using System;

namespace CGame.Animation
{
    [Serializable]
    public class AnimationDurationNotify : AnimationNotify
    {
        public virtual void OnBegin(AnimationEventContext context)
        {
        }

        public virtual void OnTick(AnimationEventContext context)
        {
        }

        public virtual void OnEnd(AnimationEventContext context, AnimationNotifyEndReason reason)
        {
        }
    }
}
