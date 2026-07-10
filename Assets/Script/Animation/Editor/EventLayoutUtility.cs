using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGame.Animation.Editor
{
    public readonly struct EventLayoutInfo
    {
        public EventLayoutInfo(AnimationNotifyEvent notifyEvent, int eventIndex, Rect rect, int lane)
        {
            NotifyEvent = notifyEvent;
            EventIndex = eventIndex;
            Rect = rect;
            Lane = lane;
        }

        public AnimationNotifyEvent NotifyEvent { get; }
        public int EventIndex { get; }
        public Rect Rect { get; }
        public int Lane { get; }
    }

    public static class EventLayoutUtility
    {
        public static Rect GetEventRect(Rect laneRect, int startFrame, int durationFrames, float frameWidth, float instantEventWidth = AnimationEditorConstants.InstantEventWidth)
        {
            float x = TimeUtility.FrameToX(laneRect, startFrame, frameWidth);
            float width = durationFrames <= 0
                ? Mathf.Max(AnimationEditorConstants.InstantEventWidth, instantEventWidth)
                : Mathf.Max(AnimationEditorConstants.MinimumEventWidth, durationFrames * frameWidth);
            return new Rect(x, laneRect.y + AnimationEditorConstants.EventVerticalPadding, width, laneRect.height - AnimationEditorConstants.EventVerticalPadding * 2f);
        }

        public static IReadOnlyList<EventLayoutInfo> LayoutEvents(IReadOnlyList<AnimationNotifyEvent> notifyEvents, Rect laneRect, float frameWidth, Func<AnimationNotifyEvent, float> getInstantEventWidth = null)
        {
            var layouts = new List<EventLayoutInfo>();
            var laneEndFrames = new List<int>();

            for (int i = 0; i < notifyEvents.Count; i++)
            {
                AnimationNotifyEvent notifyEvent = notifyEvents[i];
                float instantEventWidth = notifyEvent.IsDuration || getInstantEventWidth == null
                    ? AnimationEditorConstants.InstantEventWidth
                    : getInstantEventWidth(notifyEvent);
                int endFrame = notifyEvent.IsDuration
                    ? notifyEvent.EndFrame
                    : notifyEvent.StartFrame + Mathf.Max(1, Mathf.CeilToInt(instantEventWidth / Mathf.Max(AnimationEditorConstants.MinTimelineFrameWidth, frameWidth)));
                int lane = FindAvailableLane(laneEndFrames, notifyEvent.StartFrame);
                if (lane == laneEndFrames.Count)
                {
                    laneEndFrames.Add(endFrame);
                }
                else
                {
                    laneEndFrames[lane] = endFrame;
                }

                Rect rect = GetEventRect(laneRect, notifyEvent.StartFrame, notifyEvent.DurationFrames, frameWidth, instantEventWidth);
                if (lane > 0)
                {
                    rect.y += lane * AnimationEditorConstants.EventLaneOffset;
                    rect.height = Mathf.Max(8f, rect.height - lane * 2f);
                }

                layouts.Add(new EventLayoutInfo(notifyEvent, i, rect, lane));
            }

            return layouts;
        }

        private static int FindAvailableLane(IReadOnlyList<int> laneEndFrames, int startFrame)
        {
            for (int i = 0; i < laneEndFrames.Count; i++)
            {
                if (laneEndFrames[i] <= startFrame)
                {
                    return i;
                }
            }

            return laneEndFrames.Count;
        }
    }
}
