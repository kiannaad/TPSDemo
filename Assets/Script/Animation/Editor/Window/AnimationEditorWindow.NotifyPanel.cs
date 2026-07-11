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
        private void DrawAssetHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField("Asset", GUILayout.Width(42f));
                EditorGUI.BeginChangeCheck();
                var selected = (AnimationAssetBase)EditorGUILayout.ObjectField(state.EditingAsset, typeof(AnimationAssetBase), false);
                if (EditorGUI.EndChangeCheck())
                {
                    SetAsset(selected);
                }

                if (GUILayout.Button("Use Selection", EditorStyles.toolbarButton, GUILayout.Width(92f)))
                {
                    SyncFromSelection();
                }
            }
        }

        private void DrawClipStatus()
        {
            AnimationClip clip = state.EditingAsset.MainClip;
            if (clip == null)
            {
                EditorGUILayout.HelpBox("This asset has no AnimationClip. Notify editing is disabled until a clip is assigned.", MessageType.Warning);
                return;
            }

            float frameRate = Mathf.Max(1f, clip.frameRate);
            int totalFrames = Mathf.Max(0, Mathf.RoundToInt(clip.length * frameRate));
            EditorGUILayout.LabelField("Clip", clip.name);
            EditorGUILayout.LabelField("Length", $"{clip.length:0.###}s / {totalFrames} frames @ {frameRate:0.##} fps");
        }

        private void DrawNotifyTracks(SerializedProperty tracks)
        {
            EditorGUILayout.Space(6f);
            EnsureSelectedTrackInRange(tracks);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(TrackHeaderWidth)))
                {
                    DrawNotifyTrackManagement(tracks);
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    DrawSelectedTrackEvents(tracks);
                }
            }
        }

        private void DrawNotifyTrackManagement(SerializedProperty tracks)
        {
            EditorGUILayout.LabelField("Notifies", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ Track"))
                {
                    AddNotifyTrackToAsset("Notify Track");
                }

                using (new EditorGUI.DisabledScope(!HasNotifyTrack(state.SelectedNotifyTrackIndex)))
                {
                    if (GUILayout.Button("-"))
                    {
                        RemoveSelectedNotifyTrack();
                    }
                }
            }

            if (tracks.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Add a track before adding events.", MessageType.Info);
                return;
            }

            for (int i = 0; i < tracks.arraySize; i++)
            {
                SerializedProperty track = tracks.GetArrayElementAtIndex(i);
                SerializedProperty name = track.FindPropertyRelative("name");
                GUIStyle style = i == state.SelectedNotifyTrackIndex ? EditorStyles.toolbarButton : EditorStyles.miniButton;

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Toggle(i == state.SelectedNotifyTrackIndex, string.Empty, style, GUILayout.Width(20f)))
                    {
                        SelectNotifyTrack(i);
                    }

                    if (i == state.SelectedNotifyTrackIndex)
                    {
                        EditorGUI.BeginChangeCheck();
                        string newName = EditorGUILayout.TextField(name.stringValue);
                        if (EditorGUI.EndChangeCheck())
                        {
                            RenameSelectedNotifyTrack(newName);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button(name.stringValue, EditorStyles.miniButton))
                        {
                            SelectNotifyTrack(i);
                        }
                    }
                }
            }
        }

        private void DrawSelectedTrackEvents(SerializedProperty tracks)
        {
            EditorGUILayout.LabelField("Selected Track Events", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!HasNotifyTrack(state.SelectedNotifyTrackIndex)))
                {
                    if (GUILayout.Button("Add Notify", GUILayout.Width(96f)))
                    {
                        AddNotifyToSelectedTrack();
                    }

                    if (GUILayout.Button("Add NotifyState", GUILayout.Width(120f)))
                    {
                        AddNotifyStateToSelectedTrack();
                    }

                    if (GUILayout.Button("Custom", GUILayout.Width(78f)))
                    {
                        ShowNotifyTypeMenuForSelectedTrack();
                    }
                }
            }

            if (!HasNotifyTrack(state.SelectedNotifyTrackIndex))
            {
                EditorGUILayout.HelpBox("Select or create a track to add events.", MessageType.Info);
                return;
            }

            SerializedProperty selectedTrack = tracks.GetArrayElementAtIndex(state.SelectedNotifyTrackIndex);
            SerializedProperty events = selectedTrack.FindPropertyRelative("events");
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(90f));
            DrawTrackEvents(events);
            EditorGUILayout.EndScrollView();
        }

        private void DrawEventContextPanel()
        {
            if (!eventContextPanelOpen || !CanEditSelectedAsset)
            {
                return;
            }

            AnimationNotifyEvent notifyEvent = GetSelectedNotifyEvent();
            if (notifyEvent == null)
            {
                eventContextPanelOpen = false;
                return;
            }

            Event current = Event.current;
            if (current.type == EventType.MouseDown && !eventContextPanelRect.Contains(current.mousePosition))
            {
                eventContextPanelOpen = false;
                Repaint();
                return;
            }

            float frameRate = Mathf.Max(1f, state.EditingAsset.MainClip.frameRate);
            var values = new TimelineEventEditValues(
                (float)TimeUtility.FrameToTime(notifyEvent.StartFrame, frameRate),
                notifyEvent.StartFrame,
                notifyEvent.EndFrame,
                notifyEvent.MinTriggerWeight);
            var actions = new TimelineEventEditActions(
                beginTime => SetSelectedEventStartFrame(TimeUtility.TimeToFrame(beginTime, frameRate)),
                SetSelectedEventStartFrame,
                SetSelectedEventEndFrame,
                SetSelectedEventMinTriggerWeight,
                () => DeleteSelectedNotifyEvent());
            if (TimelineEventGUI.DrawContextPanel(eventContextPanelRect, values, notifyEvent.IsDuration, actions))
            {
                GUIUtility.ExitGUI();
            }
        }

        private void DrawTrackEvents(SerializedProperty events)
        {
            for (int i = 0; i < events.arraySize; i++)
            {
                SerializedProperty notifyEvent = events.GetArrayElementAtIndex(i);
                SerializedProperty notify = notifyEvent.FindPropertyRelative("notify");
                SerializedProperty startFrame = notifyEvent.FindPropertyRelative("startFrame");
                SerializedProperty durationFrames = notifyEvent.FindPropertyRelative("durationFrames");
                SerializedProperty minTriggerWeight = notifyEvent.FindPropertyRelative("minTriggerWeight");

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(GetNotifyLabel(notify), GUILayout.Width(TrackHeaderWidth));
                    startFrame.intValue = Mathf.Max(0, EditorGUILayout.IntField(startFrame.intValue, GUILayout.Width(FrameWidth)));
                    durationFrames.intValue = Mathf.Max(0, EditorGUILayout.IntField(durationFrames.intValue, GUILayout.Width(FrameWidth)));
                    minTriggerWeight.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(minTriggerWeight.floatValue, GUILayout.Width(FrameWidth)));
                    EditorGUILayout.PropertyField(notify, GUIContent.none, true);

                    if (GUILayout.Button("X", GUILayout.Width(24f)))
                    {
                        events.DeleteArrayElementAtIndex(i);
                    }
                }
            }
        }

        private void DrawRenameTrackField(Rect labelRect)
        {
            string controlName = GetRenameControlName(renamingTrackIndex);
            GUI.SetNextControlName(controlName);
            renamingTrackName = EditorGUI.TextField(labelRect, renamingTrackName);

            Event current = Event.current;
            if (current.type == EventType.KeyDown && (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter))
            {
                CommitRenameTrack();
                current.Use();
            }
            else if (current.type == EventType.MouseDown && !labelRect.Contains(current.mousePosition))
            {
                CommitRenameTrack();
            }
        }

    }
}
