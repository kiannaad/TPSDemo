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
        private void HandleNotifyGroupInput(TrackLayout groupLayout)
        {
            Event current = Event.current;
            if (!groupLayout.RowRect.Contains(current.mousePosition))
            {
                return;
            }

            if (current.type == EventType.MouseDown && current.button == 0)
            {
                state.ToggleNotifyTracksExpanded();
                current.Use();
                Repaint();
            }
            else if (IsContextMenuEvent(current))
            {
                ShowNotifyGroupContextMenu();
                current.Use();
            }
        }

        private void HandleTrackInput(TrackLayout trackLayout, int trackIndex, IReadOnlyList<EventLayoutInfo> eventLayouts)
        {
            Event current = Event.current;
            if (!trackLayout.RowRect.Contains(current.mousePosition))
            {
                return;
            }

            if (current.type == EventType.MouseDown && current.button == 0)
            {
                SelectNotifyTrack(trackIndex);
                if (current.clickCount == 2 && trackLayout.HeaderRect.Contains(current.mousePosition))
                {
                    BeginRenameTrack(trackIndex);
                    current.Use();
                }
            }
            else if (IsContextMenuEvent(current))
            {
                SelectNotifyTrack(trackIndex);
                bool handled = false;
                if (trackLayout.HeaderRect.Contains(current.mousePosition))
                {
                    ShowTrackNodeContextMenu(trackIndex);
                    handled = true;
                }
                else if (trackLayout.LaneRect.Contains(current.mousePosition) && !IsMouseOverEvent(eventLayouts, current.mousePosition))
                {
                    int frame = TimeUtility.XToFrame(trackLayout.LaneRect, current.mousePosition.x, TimeUtility.GetTotalFrames(state.EditingAsset.MainClip), state.TimelineFrameWidth);
                    ShowNotifyItemContextMenu(trackIndex, frame);
                    handled = true;
                }

                if (handled)
                {
                    current.Use();
                }
            }
        }

        private static bool IsMouseOverEvent(IReadOnlyList<EventLayoutInfo> eventLayouts, Vector2 mousePosition)
        {
            return eventLayouts.Any(eventLayout => eventLayout.Rect.Contains(mousePosition));
        }

        private void HandleTimelineContextMenu(TimelineLayout layout, SerializedProperty tracks)
        {
            Event current = Event.current;
            if (!CanEditSelectedAsset || !IsContextMenuEvent(current))
            {
                return;
            }

            Rect emptyTimelineRect;
            if (tracks.arraySize == 0)
            {
                TrackLayout groupLayout = TimelineLayoutUtility.GetNotifyGroupLayout(layout);
                emptyTimelineRect = new Rect(layout.TimelineRect.x, groupLayout.RowRect.yMax, layout.TimelineRect.width, layout.TimelineRect.yMax - groupLayout.RowRect.yMax);
            }
            else
            {
                TrackLayout lastVisibleLayout = state.NotifyTracksExpanded
                    ? TimelineLayoutUtility.GetTrackLayout(layout, tracks.arraySize - 1)
                    : TimelineLayoutUtility.GetNotifyGroupLayout(layout);
                emptyTimelineRect = new Rect(layout.TimelineRect.x, lastVisibleLayout.RowRect.yMax, layout.TimelineRect.width, layout.TimelineRect.yMax - lastVisibleLayout.RowRect.yMax);
            }

            if (!emptyTimelineRect.Contains(current.mousePosition) || current.mousePosition.x >= layout.ContentRect.x)
            {
                return;
            }

            ShowNotifyGroupContextMenu();
            current.Use();
        }

        private static bool IsContextMenuEvent(Event current)
        {
            return current.type == EventType.ContextClick || current.type == EventType.MouseDown && current.button == 1;
        }

        private void HandleScrubber(TimelineLayout layout)
        {
            Event current = Event.current;
            if (current.type == EventType.MouseDown && layout.RulerRect.Contains(current.mousePosition))
            {
                state.BeginDrag(-1, -1, AnimationEditorDragMode.Scrub);
                SetPreviewFrame(TimeUtility.XToFrame(layout.RulerRect, current.mousePosition.x, layout.TotalFrames, state.TimelineFrameWidth));
                current.Use();
            }

            if (state.ActiveDragMode == AnimationEditorDragMode.Scrub && current.type == EventType.MouseDrag)
            {
                SetPreviewFrame(TimeUtility.XToFrame(layout.RulerRect, current.mousePosition.x, layout.TotalFrames, state.TimelineFrameWidth));
                current.Use();
            }

            if (state.ActiveDragMode == AnimationEditorDragMode.Scrub && current.type == EventType.MouseUp)
            {
                state.EndDrag();
                current.Use();
            }
        }

        private void HandleTimelineEvent(Rect eventRect, SerializedProperty notifyEvent, int trackIndex, int eventIndex, int totalFrames, bool durationEvent)
        {
            Event current = Event.current;
            if (current.type == EventType.MouseDown && eventRect.Contains(current.mousePosition))
            {
                SelectNotifyEvent(trackIndex, eventIndex);
                if (current.button == 1)
                {
                    ShowEventContextPanel(current.mousePosition, trackIndex, eventIndex);
                    current.Use();
                    return;
                }

                float localX = current.mousePosition.x - eventRect.x;
                AnimationEditorDragMode dragMode = GetTimelineEventDragMode(localX, eventRect.width, durationEvent);
                state.BeginDrag(trackIndex, eventIndex, dragMode);
                eventContextPanelOpen = false;
                current.Use();
            }

            if (state.ActiveDragTrack != trackIndex || state.ActiveDragEvent != eventIndex)
            {
                return;
            }

            Rect laneRect = new Rect(eventRect.xMin - notifyEvent.FindPropertyRelative("startFrame").intValue * state.TimelineFrameWidth, eventRect.y, position.width - TrackHeaderWidth, eventRect.height);
            if (current.type == EventType.MouseDrag)
            {
                int frame = TimeUtility.XToFrame(laneRect, current.mousePosition.x, totalFrames, state.TimelineFrameWidth);
                ApplyEventDrag(trackIndex, eventIndex, notifyEvent, frame);
                current.Use();
            }

            if (current.type == EventType.MouseUp)
            {
                state.EndDrag();
                current.Use();
            }
        }

        private void SelectNotifyEvent(int trackIndex, int eventIndex)
        {
            SelectNotifyTrack(trackIndex);
            state.SetSelectedNotifyEvent(trackIndex, eventIndex);
            SetPreviewFrame(GetNotifyEvent(trackIndex, eventIndex)?.StartFrame ?? state.PreviewFrame);
        }

        private void ShowEventContextPanel(Vector2 mousePosition, int trackIndex, int eventIndex)
        {
            state.SetSelectedNotifyEvent(trackIndex, eventIndex);
            float x = Mathf.Min(mousePosition.x, Mathf.Max(0f, position.width - TimelineEventGUI.ContextPanelWidth - 8f));
            float y = Mathf.Min(mousePosition.y, Mathf.Max(0f, position.height - TimelineEventGUI.ContextPanelHeight - 8f));
            eventContextPanelRect = new Rect(x, y, TimelineEventGUI.ContextPanelWidth, TimelineEventGUI.ContextPanelHeight);
            eventContextPanelOpen = true;
            Repaint();
        }

        private void HandleSelectedEventKeyboard()
        {
            Event current = Event.current;
            if (!CanEditSelectedAsset || current.type != EventType.KeyDown || current.keyCode != KeyCode.Delete)
            {
                return;
            }

            if (GetSelectedNotifyEvent() == null)
            {
                return;
            }

            DeleteSelectedNotifyEvent();
            current.Use();
        }

        private void ApplyEventDrag(int trackIndex, int eventIndex, SerializedProperty notifyEventProperty, int frame)
        {
            AnimationNotifyEvent notifyEvent = GetNotifyEvent(trackIndex, eventIndex);
            if (notifyEvent == null)
            {
                return;
            }

            if (state.ActiveDragMode == AnimationEditorDragMode.MoveEvent)
            {
                eventService.MoveEvent(state.EditingAsset, notifyEvent, frame);
            }
            else if (state.ActiveDragMode == AnimationEditorDragMode.ResizeStart || state.ActiveDragMode == AnimationEditorDragMode.ResizeEnd)
            {
                eventService.TrimEvent(state.EditingAsset, notifyEvent, frame, state.ActiveDragMode);
            }

            notifyEventProperty.serializedObject.Update();
            SetPreviewFrame(notifyEvent.StartFrame);
        }

    }
}
