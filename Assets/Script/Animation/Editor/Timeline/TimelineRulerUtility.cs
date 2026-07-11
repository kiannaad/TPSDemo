using UnityEngine;

namespace CGame.Animation.Editor
{
    public enum TimelineRulerTickStyle
    {
        Hidden,
        Dot,
        Bar
    }

    public static class TimelineRulerUtility
    {
        public static int GetMajorFrameStep(int totalFrames, float frameWidth)
        {
            const float targetMajorSpacing = 80f;
            int framesPerMajor = Mathf.Max(1, Mathf.RoundToInt(targetMajorSpacing / Mathf.Max(1f, frameWidth)));
            return Mathf.Min(Mathf.Max(1, totalFrames), framesPerMajor);
        }

        public static TimelineRulerTickStyle GetMinorTickStyle(float frameWidth)
        {
            if (frameWidth < AnimationEditorConstants.MinorTickDotFrameWidth)
            {
                return TimelineRulerTickStyle.Hidden;
            }

            if (frameWidth < AnimationEditorConstants.MinorTickBarFrameWidth)
            {
                return TimelineRulerTickStyle.Dot;
            }

            return TimelineRulerTickStyle.Bar;
        }
    }
}
