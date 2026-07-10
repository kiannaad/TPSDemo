using UnityEngine;

namespace CGame.Animation.Editor
{
    public readonly struct TimelineLayout
    {
        public TimelineLayout(Rect rootRect, Rect previewRect, Rect timelineRect, Rect headerRect, Rect rulerRect, Rect contentRect, int totalFrames, int trackCount, float frameWidth)
        {
            RootRect = rootRect;
            PreviewRect = previewRect;
            TimelineRect = timelineRect;
            HeaderRect = headerRect;
            RulerRect = rulerRect;
            ContentRect = contentRect;
            TotalFrames = totalFrames;
            TrackCount = trackCount;
            FrameWidth = frameWidth;
        }

        public Rect RootRect { get; }
        public Rect PreviewRect { get; }
        public Rect TimelineRect { get; }
        public Rect HeaderRect { get; }
        public Rect RulerRect { get; }
        public Rect ContentRect { get; }
        public int TotalFrames { get; }
        public int TrackCount { get; }
        public float FrameWidth { get; }
    }

    public readonly struct TrackLayout
    {
        public TrackLayout(Rect headerRect, Rect laneRect, Rect rowRect)
        {
            HeaderRect = headerRect;
            LaneRect = laneRect;
            RowRect = rowRect;
        }

        public Rect HeaderRect { get; }
        public Rect LaneRect { get; }
        public Rect RowRect { get; }
    }

    public static class TimelineLayoutUtility
    {
        public static TimelineLayout Calculate(Rect windowRect, int trackCount, int totalFrames)
        {
            float width = Mathf.Max(360f, windowRect.width - AnimationEditorConstants.TimelineOuterPadding * 2f);
            float height = Mathf.Max(260f, windowRect.height - AnimationEditorConstants.TimelineOuterPadding * 2f);
            Rect rootRect = new Rect(AnimationEditorConstants.TimelineOuterPadding, AnimationEditorConstants.TimelineOuterPadding, width, height);

            float previewHeight = Mathf.Clamp(height * AnimationEditorConstants.PreviewHeightRatio, AnimationEditorConstants.MinPreviewHeight, AnimationEditorConstants.MaxPreviewHeight);
            Rect previewRect = new Rect(rootRect.x, rootRect.y, rootRect.width, previewHeight);
            float timelineY = previewRect.yMax + AnimationEditorConstants.TimelineOuterPadding;
            float minTimelineHeight = AnimationEditorConstants.TimelineHeaderHeight + AnimationEditorConstants.TimelineRulerHeight + GetTrackHeight(trackCount);
            float timelineHeight = Mathf.Max(minTimelineHeight, rootRect.yMax - timelineY);
            Rect timelineRect = new Rect(rootRect.x, timelineY, rootRect.width, timelineHeight);

            Rect headerRect = new Rect(timelineRect.x, timelineRect.y, AnimationEditorConstants.TrackHeaderWidth, AnimationEditorConstants.TimelineHeaderHeight + AnimationEditorConstants.TimelineRulerHeight);
            Rect rulerRect = new Rect(headerRect.xMax, timelineRect.y + AnimationEditorConstants.TimelineHeaderHeight, timelineRect.width - AnimationEditorConstants.TrackHeaderWidth, AnimationEditorConstants.TimelineRulerHeight);
            Rect contentRect = new Rect(rulerRect.x, rulerRect.yMax, rulerRect.width, timelineRect.yMax - rulerRect.yMax);
            float frameWidth = TimeUtility.CalculateFitFrameWidth(totalFrames, rulerRect.width);

            return new TimelineLayout(rootRect, previewRect, timelineRect, headerRect, rulerRect, contentRect, totalFrames, trackCount, frameWidth);
        }

        public static TrackLayout GetTrackLayout(TimelineLayout layout, int trackIndex)
        {
            float trackHeight = GetTrackHeight(layout.TrackCount);
            float y = layout.ContentRect.y + trackHeight + trackIndex * trackHeight;
            Rect headerRect = new Rect(layout.TimelineRect.x, y, AnimationEditorConstants.TrackHeaderWidth, trackHeight - AnimationEditorConstants.TrackGap);
            Rect laneRect = new Rect(layout.ContentRect.x, y, layout.ContentRect.width, trackHeight - AnimationEditorConstants.TrackGap);
            Rect rowRect = new Rect(layout.TimelineRect.x, y, layout.TimelineRect.width, trackHeight - AnimationEditorConstants.TrackGap);
            return new TrackLayout(headerRect, laneRect, rowRect);
        }

        public static TrackLayout GetNotifyGroupLayout(TimelineLayout layout)
        {
            float trackHeight = GetTrackHeight(layout.TrackCount);
            Rect headerRect = new Rect(layout.TimelineRect.x, layout.ContentRect.y, AnimationEditorConstants.TrackHeaderWidth, trackHeight - AnimationEditorConstants.TrackGap);
            Rect laneRect = new Rect(layout.ContentRect.x, layout.ContentRect.y, layout.ContentRect.width, trackHeight - AnimationEditorConstants.TrackGap);
            Rect rowRect = new Rect(layout.TimelineRect.x, layout.ContentRect.y, layout.TimelineRect.width, trackHeight - AnimationEditorConstants.TrackGap);
            return new TrackLayout(headerRect, laneRect, rowRect);
        }

        private static float GetTrackHeight(int trackCount)
        {
            if (trackCount <= 0)
            {
                return AnimationEditorConstants.TrackHeight;
            }

            return AnimationEditorConstants.TrackHeight;
        }
    }
}
