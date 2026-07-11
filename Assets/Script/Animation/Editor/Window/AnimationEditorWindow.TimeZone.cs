using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static CGame.Animation.Editor.AnimationEditorConstants;

namespace CGame.Animation.Editor
{
    public partial class AnimationEditorWindow
    {
        private void DrawEditorSurface(SerializedProperty tracks)
        {
            AnimationClip clip = state.EditingAsset.MainClip;
            AnimationSequenceAsset sequence = state.EditingAsset as AnimationSequenceAsset;
            int totalFrames = sequence == null ? TimeUtility.GetTotalFrames(clip) : GetSequenceTotalFrames(sequence);
            int trackCount = sequence == null ? tracks.arraySize : Mathf.Max(1, sequence.Clips.Length);
            var layout = TimelineLayoutUtility.Calculate(position, trackCount, totalFrames);
            SetTimelineFrameWidth(layout.FrameWidth);
            DrawStaticPreviewPanel(layout.PreviewRect);
            if (sequence != null)
            {
                DrawSequenceOverview(layout, sequence);
            }
            else
            {
                DrawTimeline(layout, tracks);
            }
        }

        private void DrawTimeline(TimelineLayout layout, SerializedProperty tracks)
        {
            DrawTimelineBackground(layout);
            HandleScrubber(layout);
            HandleTimelineContextMenu(layout, tracks);
            DrawNotifyGroupRow(layout, tracks);

            if (state.NotifyTracksExpanded)
            {
                for (int trackIndex = 0; trackIndex < tracks.arraySize; trackIndex++)
                {
                    DrawTimelineTrack(layout, tracks.GetArrayElementAtIndex(trackIndex), trackIndex);
                }
            }

            DrawTimelineEmptyStates(layout, tracks);
            DrawScrubber(layout);
        }

        private void DrawSequenceOverview(TimelineLayout layout, AnimationSequenceAsset sequence)
        {
            DrawTimelineBackground(layout);
            DrawSequenceGroupRow(layout, sequence);

            int startFrame = 0;
            AnimationSequenceAsset.SequenceEntry[] entries = sequence.Clips;
            if (entries == null || entries.Length == 0)
            {
                GUI.Label(new Rect(layout.ContentRect.x, layout.ContentRect.y + 12f, layout.ContentRect.width, 24f), "No clips in this sequence", EditorStyles.centeredGreyMiniLabel);
                TimelineTrackGUI.DrawFiller(layout, layout.ContentRect.y + AnimationEditorConstants.TrackHeight);
                DrawScrubber(layout);
                return;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                AnimationSequenceAsset.SequenceEntry entry = entries[i];
                AnimationClipAsset clipAsset = entry?.ClipAsset;
                AnimationClip clip = clipAsset == null ? null : clipAsset.MainClip;
                int clipFrames = Mathf.Max(1, TimeUtility.GetTotalFrames(clip));
                TrackLayout trackLayout = TimelineLayoutUtility.GetTrackLayout(layout, i);
                bool hovered = trackLayout.RowRect.Contains(Event.current.mousePosition);
                TimelineTrackGUI.DrawTrackRow(trackLayout, new TimelineTrackVisualState(hovered, false, i));
                DrawSequenceClipRow(trackLayout, clipAsset, clip, startFrame, clipFrames, entry?.Speed ?? 1f);
                startFrame += clipFrames;
            }

            float lastTrackY = TimelineLayoutUtility.GetTrackLayout(layout, entries.Length - 1).RowRect.yMax;
            TimelineTrackGUI.DrawFiller(layout, lastTrackY);
            DrawScrubber(layout);
        }

        private void DrawSequenceGroupRow(TimelineLayout layout, AnimationSequenceAsset sequence)
        {
            TrackLayout groupLayout = TimelineLayoutUtility.GetNotifyGroupLayout(layout);
            bool hovered = groupLayout.RowRect.Contains(Event.current.mousePosition);
            EditorGUI.DrawRect(groupLayout.HeaderRect, new Color(0.18f, 0.18f, 0.18f));
            EditorGUI.DrawRect(groupLayout.LaneRect, new Color(0.095f, 0.105f, 0.11f));
            if (hovered)
            {
                EditorGUI.DrawRect(groupLayout.RowRect, new Color(1f, 1f, 1f, 0.045f));
            }

            int count = sequence.Clips == null ? 0 : sequence.Clips.Length;
            GUI.Label(new Rect(groupLayout.HeaderRect.x + 12f, groupLayout.HeaderRect.y + 8f, groupLayout.HeaderRect.width - 54f, 18f), "Sequence", EditorStyles.miniBoldLabel);
            GUI.Label(new Rect(groupLayout.HeaderRect.xMax - 42f, groupLayout.HeaderRect.y + 8f, 34f, 18f), count.ToString(), EditorStyles.centeredGreyMiniLabel);
            GUI.Label(new Rect(groupLayout.LaneRect.x + 8f, groupLayout.LaneRect.y + 10f, groupLayout.LaneRect.width - 16f, 18f), "Read-only sequence overview. Edit notifies on each AnimationClipAsset.", EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawSequenceClipRow(TrackLayout trackLayout, AnimationClipAsset clipAsset, AnimationClip clip, int startFrame, int frameCount, float speed)
        {
            string clipName = clipAsset == null ? "Missing ClipAsset" : clipAsset.name;
            GUI.Label(TimelineTrackGUI.GetTrackNameRect(trackLayout.HeaderRect), clipName, EditorStyles.miniBoldLabel);
            GUI.Label(new Rect(trackLayout.HeaderRect.xMax - 54f, trackLayout.HeaderRect.y + 8f, 48f, 18f), $"x{speed:0.##}", EditorStyles.centeredGreyMiniLabel);

            Rect clipRect = EventLayoutUtility.GetEventRect(trackLayout.LaneRect, startFrame, frameCount, state.TimelineFrameWidth, 0f);
            Color clipColor = clipAsset == null ? new Color(0.36f, 0.18f, 0.18f) : new Color(0.38f, 0.42f, 0.48f);
            Color swatchColor = clipAsset == null ? new Color(0.8f, 0.25f, 0.25f) : new Color(0.65f, 0.7f, 0.78f);
            string label = clip == null ? "Missing AnimationClip" : $"{clip.name}  {frameCount}f";
            var visualState = new TimelineEventGUIState(clipRect.Contains(Event.current.mousePosition), false, false);
            TimelineEventGUI.DrawEvent(new TimelineEventVisual(clipRect, label, true, clipColor, swatchColor, visualState));

            int notifyCount = clipAsset == null ? 0 : clipAsset.NotifyTracks.Sum(track => track.Events.Count);
            GUI.Label(new Rect(clipRect.x + 8f, clipRect.yMax - 16f, Mathf.Max(40f, clipRect.width - 16f), 14f), $"{notifyCount} notifies", EditorStyles.whiteMiniLabel);
        }

        private void DrawTimelineBackground(TimelineLayout layout)
        {
            TimelineTrackGUI.DrawTimelineBackground(layout, "Tracks");

            int majorStep = TimelineRulerUtility.GetMajorFrameStep(layout.TotalFrames, state.TimelineFrameWidth);
            TimelineRulerTickStyle minorTickStyle = TimelineRulerUtility.GetMinorTickStyle(state.TimelineFrameWidth);
            for (int frame = 0; frame <= layout.TotalFrames; frame++)
            {
                float x = TimeUtility.FrameToX(layout.RulerRect, frame, state.TimelineFrameWidth);
                bool major = frame % majorStep == 0 || frame == layout.TotalFrames;
                if (major)
                {
                    EditorGUI.DrawRect(new Rect(x, layout.TimelineRect.y, 1f, layout.TimelineRect.height), new Color(0.36f, 0.36f, 0.36f));
                    GUI.Label(new Rect(x + 3f, layout.RulerRect.y + 3f, 54f, 16f), frame.ToString(), EditorStyles.miniLabel);
                    continue;
                }

                if (minorTickStyle == TimelineRulerTickStyle.Dot)
                {
                    EditorGUI.DrawRect(new Rect(x - 1f, layout.RulerRect.y + 11f, 2f, 2f), new Color(0.84f, 0.84f, 0.84f, 0.9f));
                }
                else if (minorTickStyle == TimelineRulerTickStyle.Bar)
                {
                    EditorGUI.DrawRect(new Rect(x, layout.RulerRect.y + 6f, 1f, 10f), new Color(0.9f, 0.9f, 0.9f, 0.95f));
                }
            }

            EditorGUI.DrawRect(new Rect(layout.HeaderRect.xMax - 1f, layout.HeaderRect.y, 1f, layout.TimelineRect.height), new Color(0.28f, 0.28f, 0.28f));
            EditorGUI.DrawRect(new Rect(layout.TimelineRect.x, layout.RulerRect.yMax - 1f, layout.TimelineRect.width, 1f), new Color(0.22f, 0.22f, 0.22f));
        }

        private void DrawNotifyGroupRow(TimelineLayout layout, SerializedProperty tracks)
        {
            TrackLayout groupLayout = TimelineLayoutUtility.GetNotifyGroupLayout(layout);
            bool hover = groupLayout.RowRect.Contains(Event.current.mousePosition);
            HandleNotifyGroupInput(groupLayout);
            NotifyTrackTimelineGUI.DrawGroupRow(groupLayout, tracks.arraySize, state.NotifyTracksExpanded, hover);
        }

        private void DrawTimelineTrack(TimelineLayout layout, SerializedProperty track, int trackIndex)
        {
            SerializedProperty name = track.FindPropertyRelative("name");
            SerializedProperty events = track.FindPropertyRelative("events");
            TrackLayout trackLayout = TimelineLayoutUtility.GetTrackLayout(layout, trackIndex);
            AnimationNotifyTrack notifyTrack = state.EditingAsset.NotifyTracks[trackIndex];
            var eventLayouts = EventLayoutUtility.LayoutEvents(notifyTrack.Events, trackLayout.LaneRect, state.TimelineFrameWidth, GetInstantEventWidth);
            bool headerHover = trackLayout.HeaderRect.Contains(Event.current.mousePosition);
            bool laneHover = trackLayout.LaneRect.Contains(Event.current.mousePosition);
            HandleTrackInput(trackLayout, trackIndex, eventLayouts);

            var visualState = new TimelineTrackVisualState(headerHover || laneHover, trackIndex == state.SelectedNotifyTrackIndex, trackIndex);
            TimelineTrackGUI.DrawTrackRow(trackLayout, visualState);

            DrawTrackHeader(trackLayout.HeaderRect, name.stringValue, trackIndex);

            foreach (EventLayoutInfo eventLayout in eventLayouts.Where(eventLayout => eventLayout.NotifyEvent.IsDuration))
            {
                DrawTimelineEvent(eventLayout.Rect, events.GetArrayElementAtIndex(eventLayout.EventIndex), trackIndex, eventLayout.EventIndex, layout.TotalFrames);
            }

            foreach (EventLayoutInfo eventLayout in eventLayouts.Where(eventLayout => !eventLayout.NotifyEvent.IsDuration))
            {
                DrawTimelineEvent(eventLayout.Rect, events.GetArrayElementAtIndex(eventLayout.EventIndex), trackIndex, eventLayout.EventIndex, layout.TotalFrames);
            }

            if (events.arraySize == 0)
            {
                GUI.Label(new Rect(trackLayout.LaneRect.x + 8f, trackLayout.LaneRect.y + 10f, trackLayout.LaneRect.width - 16f, 18f), "No notifies on this track", EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void DrawTrackHeader(Rect headerRect, string trackName, int trackIndex)
        {
            Rect labelRect = TimelineTrackGUI.GetTrackNameRect(headerRect);
            if (renamingTrackIndex == trackIndex)
            {
                TimelineTrackGUI.DrawTrackHeader(headerRect, string.Empty, trackIndex, trackIndex == state.SelectedNotifyTrackIndex);
                DrawRenameTrackField(labelRect);
            }
            else
            {
                TimelineTrackGUI.DrawTrackHeader(headerRect, trackName, trackIndex, trackIndex == state.SelectedNotifyTrackIndex);
            }
        }

        private void DrawTimelineEvent(Rect eventRect, SerializedProperty notifyEvent, int trackIndex, int eventIndex, int totalFrames)
        {
            SerializedProperty notify = notifyEvent.FindPropertyRelative("notify");
            SerializedProperty durationFrames = notifyEvent.FindPropertyRelative("durationFrames");
            int duration = Mathf.Max(0, durationFrames.intValue);
            bool durationEvent = duration > 0;
            string notifyLabel = GetNotifyLabel(notify);
            Rect drawRect = eventRect;
            bool hovered = drawRect.Contains(Event.current.mousePosition);
            bool active = state.ActiveDragTrack == trackIndex && state.ActiveDragEvent == eventIndex;
            bool selected = state.SelectedNotifyEventTrack == trackIndex && state.SelectedNotifyEventIndex == eventIndex;
            Color eventColor = durationEvent ? new Color(0.9f, 0.58f, 0.22f) : new Color(0.28f, 0.56f, 0.88f);
            Color swatchColor = durationEvent ? new Color(1f, 0.76f, 0.28f) : new Color(0.38f, 0.72f, 1f);
            var guiState = new TimelineEventGUIState(hovered, active, selected);
            TimelineEventGUI.DrawEvent(new TimelineEventVisual(drawRect, notifyLabel, durationEvent, eventColor, swatchColor, guiState));

            HandleTimelineEvent(eventRect, notifyEvent, trackIndex, eventIndex, totalFrames, durationEvent);
        }

        private static float GetInstantEventWidth(AnimationNotifyEvent notifyEvent)
        {
            string notifyLabel = GetNotifyLabel(notifyEvent.Notify);
            return TimelineEventGUI.GetInstantEventWidth(notifyLabel);
        }

        private void DrawTimelineEmptyStates(TimelineLayout layout, SerializedProperty tracks)
        {
            NotifyTrackTimelineGUI.DrawEmptyStates(layout, tracks.arraySize, state.NotifyTracksExpanded);
        }

        private void DrawScrubber(TimelineLayout layout)
        {
            Rect cursorRect = new Rect(layout.RulerRect.x, layout.RulerRect.y, layout.RulerRect.width, layout.ContentRect.yMax - layout.RulerRect.y);
            float x = TimeUtility.FrameToX(cursorRect, state.PreviewFrame, state.TimelineFrameWidth);
            EditorGUI.DrawRect(new Rect(x - 1f, cursorRect.y, 2f, cursorRect.height), new Color(0.95f, 0.25f, 0.2f));
            GUI.Label(new Rect(x + 4f, layout.RulerRect.y + 2f, 72f, 18f), state.PreviewFrame.ToString(), EditorStyles.miniBoldLabel);
        }

        private static int GetSequenceTotalFrames(AnimationSequenceAsset sequence)
        {
            if (sequence?.Clips == null || sequence.Clips.Length == 0)
            {
                return 0;
            }

            int totalFrames = 0;
            for (int i = 0; i < sequence.Clips.Length; i++)
            {
                AnimationClip clip = sequence.Clips[i]?.ClipAsset?.MainClip;
                totalFrames += Mathf.Max(1, TimeUtility.GetTotalFrames(clip));
            }

            return totalFrames;
        }

    }
}
