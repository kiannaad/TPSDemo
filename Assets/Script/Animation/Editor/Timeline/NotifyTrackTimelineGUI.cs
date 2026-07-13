using UnityEditor;
using UnityEngine;

namespace CGame.Animation.Editor
{
    public static class NotifyTrackTimelineGUI
    {
        public static void DrawGroupRow(TrackLayout groupLayout, int trackCount, bool expanded, bool hovered)
        {
            EditorGUI.DrawRect(groupLayout.HeaderRect, new Color(0.18f, 0.18f, 0.18f));
            EditorGUI.DrawRect(groupLayout.LaneRect, new Color(0.095f, 0.105f, 0.11f));
            if (hovered)
            {
                EditorGUI.DrawRect(groupLayout.RowRect, new Color(1f, 1f, 1f, 0.045f));
            }

            Rect foldoutRect = new Rect(groupLayout.HeaderRect.x + 8f, groupLayout.HeaderRect.y + 10f, 12f, 12f);
            EditorGUI.Foldout(foldoutRect, expanded, GUIContent.none);
            GUI.Label(new Rect(foldoutRect.xMax + 6f, groupLayout.HeaderRect.y + 8f, groupLayout.HeaderRect.width - 48f, 18f), "Notifies", EditorStyles.miniBoldLabel);
            GUI.Label(new Rect(groupLayout.HeaderRect.xMax - 42f, groupLayout.HeaderRect.y + 8f, 34f, 18f), trackCount.ToString(), EditorStyles.centeredGreyMiniLabel);

            if (!expanded && trackCount > 0)
            {
                GUI.Label(new Rect(groupLayout.LaneRect.x + 8f, groupLayout.LaneRect.y + 10f, groupLayout.LaneRect.width - 16f, 18f), $"{trackCount} notify tracks collapsed", EditorStyles.centeredGreyMiniLabel);
            }
        }

        public static void DrawEmptyStates(TimelineLayout layout, int trackCount, bool expanded)
        {
            if (!expanded)
            {
                TrackLayout groupLayout = TimelineLayoutUtility.GetNotifyGroupLayout(layout);
                TimelineTrackGUI.DrawFiller(layout, groupLayout.RowRect.yMax);
                return;
            }

            if (trackCount == 0)
            {
                TrackLayout groupLayout = TimelineLayoutUtility.GetNotifyGroupLayout(layout);
                GUI.Label(new Rect(layout.ContentRect.x, groupLayout.RowRect.yMax + 12f, layout.ContentRect.width, 24f), "No notify tracks", EditorStyles.centeredGreyMiniLabel);
                TimelineTrackGUI.DrawFiller(layout, groupLayout.RowRect.yMax);
                return;
            }

            float lastTrackY = TimelineLayoutUtility.GetTrackLayout(layout, trackCount - 1).RowRect.yMax;
            TimelineTrackGUI.DrawFiller(layout, lastTrackY);
        }
    }
}
