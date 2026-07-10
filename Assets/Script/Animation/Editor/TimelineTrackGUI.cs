using UnityEditor;
using UnityEngine;
using static CGame.Animation.Editor.AnimationEditorConstants;

namespace CGame.Animation.Editor
{
    public readonly struct TimelineTrackVisualState
    {
        public TimelineTrackVisualState(bool hovered, bool selected, int rowIndex)
        {
            Hovered = hovered;
            Selected = selected;
            RowIndex = rowIndex;
        }

        public bool Hovered { get; }
        public bool Selected { get; }
        public int RowIndex { get; }
    }

    public static class TimelineTrackGUI
    {
        public static void DrawTimelineBackground(TimelineLayout layout, string headerLabel)
        {
            EditorGUI.DrawRect(layout.TimelineRect, new Color(0.12f, 0.12f, 0.12f));
            EditorGUI.DrawRect(layout.HeaderRect, new Color(0.17f, 0.17f, 0.17f));
            GUI.Label(new Rect(layout.HeaderRect.x + 8f, layout.HeaderRect.y + 4f, layout.HeaderRect.width - 16f, 18f), headerLabel, EditorStyles.miniBoldLabel);
            EditorGUI.DrawRect(layout.RulerRect, new Color(0.1f, 0.1f, 0.1f));
            EditorGUI.DrawRect(layout.ContentRect, new Color(0.13f, 0.13f, 0.13f));
        }

        public static void DrawTrackRow(TrackLayout trackLayout, TimelineTrackVisualState state)
        {
            EditorGUI.DrawRect(trackLayout.HeaderRect, state.RowIndex % 2 == 0 ? new Color(0.19f, 0.19f, 0.19f) : new Color(0.165f, 0.165f, 0.165f));
            EditorGUI.DrawRect(trackLayout.LaneRect, state.RowIndex % 2 == 0 ? new Color(0.105f, 0.12f, 0.125f) : new Color(0.085f, 0.1f, 0.105f));
            if (state.Hovered)
            {
                EditorGUI.DrawRect(trackLayout.RowRect, new Color(1f, 1f, 1f, 0.045f));
            }

            if (state.Selected)
            {
                EditorGUI.DrawRect(trackLayout.HeaderRect, new Color(0.18f, 0.44f, 0.72f, 0.44f));
                EditorGUI.DrawRect(trackLayout.LaneRect, new Color(0.16f, 0.34f, 0.54f, 0.24f));
            }
        }

        public static void DrawFiller(TimelineLayout layout, float y)
        {
            if (y >= layout.TimelineRect.yMax)
            {
                return;
            }

            Rect fillerRect = new Rect(layout.TimelineRect.x, y, layout.TimelineRect.width, layout.TimelineRect.yMax - y);
            EditorGUI.DrawRect(fillerRect, new Color(0.13f, 0.13f, 0.13f));
        }

        public static void DrawTrackHeader(Rect headerRect, string trackName, int trackIndex, bool selected)
        {
            Rect swatchRect = new Rect(headerRect.x, headerRect.y, TrackSwatchWidth, headerRect.height);
            EditorGUI.DrawRect(swatchRect, selected ? new Color(0.2f, 0.65f, 1f) : new Color(0.42f, 0.55f, 0.62f));

            Rect iconRect = new Rect(headerRect.x + TrackSwatchWidth + 8f, headerRect.y + (headerRect.height - TrackIconSize) * 0.5f, TrackIconSize, TrackIconSize);
            GUI.Label(iconRect, "N", EditorStyles.centeredGreyMiniLabel);

            Rect labelRect = GetTrackNameRect(headerRect);
            GUI.Label(labelRect, trackName, EditorStyles.miniBoldLabel);
        }

        public static Rect GetTrackNameRect(Rect headerRect)
        {
            float iconXMax = headerRect.x + TrackSwatchWidth + 8f + TrackIconSize;
            return new Rect(iconXMax + 6f, headerRect.y + 8f, headerRect.width - iconXMax + headerRect.x - 12f, 18f);
        }
    }
}
