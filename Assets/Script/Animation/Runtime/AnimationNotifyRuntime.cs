using System.Collections.Generic;
using UnityEngine;

namespace CGame.Animation
{
    public class AnimationNotifyRuntime
    {
        private const float DefaultWeight = 1f;

        private readonly UnityEngine.Object owner;
        private readonly AnimationClipAsset clipAsset;
        private readonly Dictionary<AnimationNotifyEvent, ActiveNotifyState> activeNotifyStates = new Dictionary<AnimationNotifyEvent, ActiveNotifyState>();

        public AnimationNotifyRuntime(UnityEngine.Object owner, AnimationClipAsset clipAsset)
        {
            this.owner = owner;
            this.clipAsset = clipAsset;
        }

        public UnityEngine.Object Owner => owner;
        public AnimationClipAsset ClipAsset => clipAsset;
        public float LastTime { get; private set; }
        public float CurrentTime { get; private set; }
        public int ActiveNotifyCount => activeNotifyStates.Count;

        public void Tick(float deltaTime)
        {
            Tick(CurrentTime + Mathf.Max(0f, deltaTime), deltaTime, DefaultWeight);
        }

        public void CaptureBeforeEvaluate()
        {
            LastTime = CurrentTime;
        }

        public void Tick(float currentTime, float deltaTime, float weight)
        {
            CaptureBeforeEvaluate();
            CurrentTime = Mathf.Max(0f, currentTime);
            DispatchAtCurrentTime(deltaTime, weight);
        }

        public void EndAll(AnimationNotifyEndReason reason)
        {
            EndAll(reason, 0f, DefaultWeight);
        }

        private void DispatchAtCurrentTime(float deltaTime, float weight)
        {
            float clampedDeltaTime = Mathf.Max(0f, deltaTime);
            float clampedWeight = Mathf.Max(0f, weight);

            if (EndActiveNotifiesIfNeeded(clampedDeltaTime, clampedWeight))
            {
                return;
            }

            if (clipAsset == null || !clipAsset.IsValid || CurrentTime < LastTime)
            {
                return;
            }

            DispatchNotifies(clampedDeltaTime, clampedWeight);
        }

        private bool EndActiveNotifiesIfNeeded(float deltaTime, float weight)
        {
            if (activeNotifyStates.Count == 0)
            {
                return false;
            }

            if (!IsOwnerEnabled())
            {
                EndAll(AnimationNotifyEndReason.OwnerDisabled, deltaTime, weight);
                return true;
            }

            return false;
        }

        private void DispatchNotifies(float deltaTime, float weight)
        {
            float frameRate = clipAsset.AnimationClip.frameRate;
            float clipLength = clipAsset.AnimationClip.length;
            if (frameRate <= 0f || clipLength <= 0f)
            {
                return;
            }

            foreach (AnimationNotifyTrack track in clipAsset.NotifyTracks)
            {
                if (track?.Events == null)
                {
                    continue;
                }

                foreach (AnimationNotifyEvent notifyEvent in track.Events)
                {
                    if (notifyEvent?.Notify is AnimationDurationNotify)
                    {
                        UpdateDurationNotify(notifyEvent, frameRate, clipLength, deltaTime, weight);
                        continue;
                    }

                    if (!ShouldDispatchInstantNotify(notifyEvent, frameRate, clipLength, weight))
                    {
                        continue;
                    }

                    DispatchInstantNotify(notifyEvent, deltaTime, weight);
                }
            }
        }

        private bool ShouldDispatchInstantNotify(AnimationNotifyEvent notifyEvent, float frameRate, float clipLength, float weight)
        {
            if (notifyEvent == null || notifyEvent.IsDuration || notifyEvent.Notify is not AnimationInstantNotify)
            {
                return false;
            }

            if (weight < notifyEvent.MinTriggerWeight)
            {
                return false;
            }

            float eventTime = notifyEvent.StartFrame / frameRate;
            return CrossedLoopingTime(eventTime, clipLength);
        }

        private void UpdateDurationNotify(AnimationNotifyEvent notifyEvent, float frameRate, float clipLength, float deltaTime, float weight)
        {
            float localStartTime = notifyEvent.StartFrame / frameRate;
            float localEndTime = notifyEvent.EndFrame / frameRate;
            if (localEndTime <= localStartTime)
            {
                return;
            }

            bool isActive = activeNotifyStates.TryGetValue(notifyEvent, out ActiveNotifyState activeState);
            if (isActive && weight < notifyEvent.MinTriggerWeight)
            {
                EndDurationNotify(activeState, AnimationNotifyEndReason.WeightBelowThreshold, deltaTime, weight);
                return;
            }

            if (isActive && CurrentTime >= activeState.EndTime)
            {
                EndDurationNotify(activeState, AnimationNotifyEndReason.NaturalEnd, deltaTime, weight);
                return;
            }

            float loopStartTime = GetLoopStartTime(localStartTime, clipLength);
            float startTime = loopStartTime + localStartTime;
            float endTime = loopStartTime + localEndTime;
            bool isInRange = CurrentTime >= startTime && CurrentTime < endTime;
            if (!isActive && isInRange)
            {
                if (weight < notifyEvent.MinTriggerWeight)
                {
                    return;
                }

                activeState = new ActiveNotifyState(notifyEvent, CurrentTime, endTime);
                activeNotifyStates.Add(notifyEvent, activeState);
                DispatchDurationBegin(activeState, deltaTime, weight);
                isActive = true;
            }

            if (isActive && activeNotifyStates.ContainsKey(notifyEvent))
            {
                DispatchDurationTick(activeState, deltaTime, weight);
            }
        }

        private void DispatchInstantNotify(AnimationNotifyEvent notifyEvent, float deltaTime, float weight)
        {
            var context = new AnimationEventContext(
                owner,
                clipAsset,
                notifyEvent,
                GetNormalizedTime(),
                deltaTime,
                weight);

            switch (notifyEvent.Notify.DispatchPolicy)
            {
                case AnimationNotifyDispatchPolicy.DirectNotify:
                    ((AnimationInstantNotify)notifyEvent.Notify).OnNotify(context);
                    break;

                case AnimationNotifyDispatchPolicy.OwnerReceiver:
                    DispatchToOwnerReceivers(context);
                    break;

                case AnimationNotifyDispatchPolicy.ContextEffectTable:
                    break;
            }
        }

        private void DispatchDurationBegin(ActiveNotifyState activeState, float deltaTime, float weight)
        {
            AnimationEventContext context = CreateContext(activeState.Event, deltaTime, weight);
            activeState.LastUpdateFrame = Time.frameCount;

            switch (activeState.Event.Notify.DispatchPolicy)
            {
                case AnimationNotifyDispatchPolicy.DirectNotify:
                    ((AnimationDurationNotify)activeState.Event.Notify).OnBegin(context);
                    break;

                case AnimationNotifyDispatchPolicy.OwnerReceiver:
                    DispatchToOwnerReceivers(context);
                    break;

                case AnimationNotifyDispatchPolicy.ContextEffectTable:
                    break;
            }
        }

        private void DispatchDurationTick(ActiveNotifyState activeState, float deltaTime, float weight)
        {
            AnimationEventContext context = CreateContext(activeState.Event, deltaTime, weight);
            activeState.LastUpdateFrame = Time.frameCount;

            switch (activeState.Event.Notify.DispatchPolicy)
            {
                case AnimationNotifyDispatchPolicy.DirectNotify:
                    ((AnimationDurationNotify)activeState.Event.Notify).OnTick(context);
                    break;

                case AnimationNotifyDispatchPolicy.OwnerReceiver:
                    DispatchToOwnerReceivers(context);
                    break;

                case AnimationNotifyDispatchPolicy.ContextEffectTable:
                    break;
            }
        }

        private void EndDurationNotify(ActiveNotifyState activeState, AnimationNotifyEndReason reason, float deltaTime, float weight)
        {
            if (!activeNotifyStates.Remove(activeState.Event))
            {
                return;
            }

            AnimationEventContext context = CreateContext(activeState.Event, deltaTime, weight);
            activeState.LastUpdateFrame = Time.frameCount;

            switch (activeState.Event.Notify.DispatchPolicy)
            {
                case AnimationNotifyDispatchPolicy.DirectNotify:
                    ((AnimationDurationNotify)activeState.Event.Notify).OnEnd(context, reason);
                    break;

                case AnimationNotifyDispatchPolicy.OwnerReceiver:
                    DispatchToOwnerReceivers(context);
                    break;

                case AnimationNotifyDispatchPolicy.ContextEffectTable:
                    break;
            }
        }

        private void EndAll(AnimationNotifyEndReason reason, float deltaTime, float weight)
        {
            if (activeNotifyStates.Count == 0)
            {
                return;
            }

            var activeStates = new List<ActiveNotifyState>(activeNotifyStates.Values);
            foreach (ActiveNotifyState activeState in activeStates)
            {
                EndDurationNotify(activeState, reason, deltaTime, weight);
            }
        }

        private AnimationEventContext CreateContext(AnimationNotifyEvent notifyEvent, float deltaTime, float weight)
        {
            return new AnimationEventContext(
                owner,
                clipAsset,
                notifyEvent,
                GetNormalizedTime(),
                deltaTime,
                weight);
        }

        private void DispatchToOwnerReceivers(AnimationEventContext context)
        {
            GameObject ownerGameObject = context.OwnerGameObject;
            if (ownerGameObject == null)
            {
                return;
            }

            foreach (IAnimationNotifyReceiver receiver in ownerGameObject.GetComponents<IAnimationNotifyReceiver>())
            {
                receiver.OnAnimationNotify(context);
            }
        }

        private float GetNormalizedTime()
        {
            float length = clipAsset != null && clipAsset.AnimationClip != null ? clipAsset.AnimationClip.length : 0f;
            return length > 0f ? CurrentTime / length : 0f;
        }

        private bool CrossedLoopingTime(float localTime, float clipLength)
        {
            if (localTime < 0f || localTime > clipLength)
            {
                return false;
            }

            int firstLoop = Mathf.FloorToInt(LastTime / clipLength);
            int lastLoop = Mathf.FloorToInt(CurrentTime / clipLength);
            for (int loopIndex = firstLoop; loopIndex <= lastLoop; loopIndex++)
            {
                float absoluteTime = loopIndex * clipLength + localTime;
                if (absoluteTime > LastTime && absoluteTime <= CurrentTime)
                {
                    return true;
                }
            }

            return false;
        }

        private float GetLoopStartTime(float localStartTime, float clipLength)
        {
            int loopIndex = Mathf.FloorToInt(CurrentTime / clipLength);
            float loopStartTime = loopIndex * clipLength;
            if (CurrentTime < loopStartTime + localStartTime && loopIndex > 0)
            {
                loopStartTime -= clipLength;
            }

            return loopStartTime;
        }

        private bool IsOwnerEnabled()
        {
            if (owner is GameObject gameObject)
            {
                return gameObject.activeInHierarchy;
            }

            if (owner is Behaviour behaviour)
            {
                return behaviour.isActiveAndEnabled;
            }

            if (owner is Component component)
            {
                return component.gameObject.activeInHierarchy;
            }

            return true;
        }

        private class ActiveNotifyState
        {
            public ActiveNotifyState(AnimationNotifyEvent notifyEvent, float enteredTime, float endTime)
            {
                Event = notifyEvent;
                EnteredTime = enteredTime;
                EndTime = endTime;
                LastUpdateFrame = Time.frameCount;
            }

            public AnimationNotifyEvent Event { get; }
            public float EnteredTime { get; }
            public float EndTime { get; }
            public int LastUpdateFrame { get; set; }
        }
    }
}
