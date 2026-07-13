using UnityEngine;

namespace CGame.Animation.Editor
{
    public static class TimeUtility
    {
        public static int GetTotalFrames(AnimationClip clip)
        {
            if (clip == null)
            {
                return 0;
            }

            return Mathf.Max(0, Mathf.RoundToInt(clip.length * Mathf.Max(1f, clip.frameRate)));
        }

        public static float CalculateFitFrameWidth(int totalFrames, float availableWidth)
        {
            return Mathf.Clamp(availableWidth / Mathf.Max(1, totalFrames), AnimationEditorConstants.MinTimelineFrameWidth, AnimationEditorConstants.MaxTimelineFrameWidth);
        }

        public static float FrameToX(Rect rect, int frame, float frameWidth)
        {
            return rect.x + frame * Mathf.Max(AnimationEditorConstants.MinTimelineFrameWidth, frameWidth);
        }

        public static int XToFrame(Rect rect, float x, int totalFrames, float frameWidth)
        {
            return Mathf.Clamp(Mathf.RoundToInt((x - rect.x) / Mathf.Max(AnimationEditorConstants.MinTimelineFrameWidth, frameWidth)), 0, Mathf.Max(0, totalFrames));
        }

        public static double FrameToTime(int frame, float frameRate)
        {
            return frame / Mathf.Max(1f, frameRate);
        }

        public static int TimeToFrame(double time, float frameRate)
        {
            return Mathf.Max(0, Mathf.RoundToInt((float)(time * Mathf.Max(1f, frameRate))));
        }
    }
}
