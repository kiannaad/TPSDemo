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
        public bool SelectNotifyTrack(int trackIndex)
        {
            if (!HasNotifyTrack(trackIndex))
            {
                return false;
            }

            state.SetSelectedNotifyTrackIndex(trackIndex);
            Repaint();
            return true;
        }

        public AnimationNotifyTrack AddNotifyTrackToAsset(string trackName = "Notify Track")
        {
            if (!CanEditSelectedAsset)
            {
                return null;
            }

            AnimationNotifyTrack track = eventService.AddTrack(state.EditingAsset, trackName);
            state.SetSelectedNotifyTrackIndex(state.EditingAsset.NotifyTracks.Count - 1);
            RebuildSerializedAsset();
            return track;
        }

#if UNITY_INCLUDE_TESTS
        public AnimationNotifyTrack InsertNotifyTrackAfterForTesting(int trackIndex)
        {
            return InsertNotifyTrackAfter(trackIndex);
        }
#endif

        public bool RenameSelectedNotifyTrack(string trackName)
        {
            if (!CanEditSelectedAsset || !HasNotifyTrack(state.SelectedNotifyTrackIndex))
            {
                return false;
            }

            bool renamed = eventService.RenameTrack(state.EditingAsset, state.SelectedNotifyTrackIndex, trackName);
            RebuildSerializedAsset();
            return renamed;
        }

        public bool RemoveSelectedNotifyTrack()
        {
            if (!CanEditSelectedAsset || !HasNotifyTrack(state.SelectedNotifyTrackIndex))
            {
                return false;
            }

            bool removed = eventService.RemoveTrack(state.EditingAsset, state.SelectedNotifyTrackIndex);
            state.EnsureSelectedTrackInRange();
            RebuildSerializedAsset();
            return removed;
        }

        public AnimationNotifyEvent AddNotifyToSelectedTrack()
        {
            return AddNotifyToSelectedTrack(new AnimationInstantNotify(), 0);
        }

        public AnimationNotifyEvent AddNotifyStateToSelectedTrack()
        {
            return AddNotifyToSelectedTrack(new AnimationDurationNotify(), DefaultNotifyStateDurationFrames);
        }

        private void SetSelectedEventStartFrame(int frame)
        {
            AnimationNotifyEvent notifyEvent = GetSelectedNotifyEvent();
            if (eventService.SetEventStartFrame(state.EditingAsset, notifyEvent, frame))
            {
                SetPreviewFrame(notifyEvent.StartFrame);
                RebuildSerializedAsset();
            }
        }

        private void SetSelectedEventEndFrame(int frame)
        {
            AnimationNotifyEvent notifyEvent = GetSelectedNotifyEvent();
            if (eventService.SetEventEndFrame(state.EditingAsset, notifyEvent, frame))
            {
                SetPreviewFrame(notifyEvent.StartFrame);
                RebuildSerializedAsset();
            }
        }

        private void SetSelectedEventMinTriggerWeight(float minTriggerWeight)
        {
            AnimationNotifyEvent notifyEvent = GetSelectedNotifyEvent();
            if (eventService.SetEventMinTriggerWeight(state.EditingAsset, notifyEvent, minTriggerWeight))
            {
                RebuildSerializedAsset();
            }
        }

        private bool DeleteSelectedNotifyEvent()
        {
            int trackIndex = state.SelectedNotifyEventTrack;
            int eventIndex = state.SelectedNotifyEventIndex;
            if (!eventService.RemoveEvent(state.EditingAsset, trackIndex, eventIndex))
            {
                return false;
            }

            state.ClearSelectedNotifyEvent();
            eventContextPanelOpen = false;
            RebuildSerializedAsset();
            return true;
        }

        private void AddNotify(SerializedProperty events, Type notifyType)
        {
            events.InsertArrayElementAtIndex(events.arraySize);
            SerializedProperty notifyEvent = events.GetArrayElementAtIndex(events.arraySize - 1);
            notifyEvent.FindPropertyRelative("startFrame").intValue = state.PreviewFrame;
            notifyEvent.FindPropertyRelative("durationFrames").intValue = notifyType == typeof(AnimationDurationNotify) ? 1 : 0;
            notifyEvent.FindPropertyRelative("notify").managedReferenceValue = Activator.CreateInstance(notifyType);
        }

        private AnimationNotifyEvent AddNotifyToSelectedTrack(AnimationNotify notify, int durationFrames)
        {
            if (!CanEditSelectedAsset || !HasNotifyTrack(state.SelectedNotifyTrackIndex))
            {
                return null;
            }

            AnimationNotifyEvent notifyEvent = eventService.AddNotify(state.EditingAsset, state.SelectedNotifyTrackIndex, notify, state.PreviewFrame, durationFrames);
            RebuildSerializedAsset();
            return notifyEvent;
        }

        private void ShowNotifyTypeMenuForSelectedTrack()
        {
            var menu = new GenericMenu();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<AnimationNotify>().Where(type => !type.IsAbstract))
            {
                Type notifyType = type;
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(notifyType.Name)), false, () =>
                {
                AddNotifyToSelectedTrack((AnimationNotify)Activator.CreateInstance(notifyType), notifyType == typeof(AnimationDurationNotify) ? DefaultNotifyStateDurationFrames : 0);
                });
            }

            menu.ShowAsContext();
        }

        private void ShowNotifyGroupContextMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add Notify Track"), false, () => AddNotifyTrackToAsset(GetNextNotifyTrackName()));
            menu.ShowAsContext();
        }

        private void ShowTrackNodeContextMenu(int trackIndex)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Insert Notify Track"), false, () => InsertNotifyTrackAfter(trackIndex));
            menu.AddItem(new GUIContent("Remove Notify Track"), false, () => RemoveNotifyTrack(trackIndex));
            menu.ShowAsContext();
        }

        private void ShowNotifyItemContextMenu(int trackIndex, int frame)
        {
            var menu = new GenericMenu();
            AddNotifyItemMenuEntries(menu, "Add Notify", trackIndex, frame, false);
            AddNotifyItemMenuEntries(menu, "Add Notify State", trackIndex, frame, true);

            menu.ShowAsContext();
        }

        private void AddNotifyItemMenuEntries(GenericMenu menu, string rootPath, int trackIndex, int frame, bool duration)
        {
            Type[] notifyTypes = GetNotifyMenuTypes(duration).ToArray();
            if (notifyTypes.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent($"{rootPath}/No Types"));
                return;
            }

            foreach (Type type in notifyTypes)
            {
                Type notifyType = type;
                menu.AddItem(new GUIContent($"{rootPath}/{ObjectNames.NicifyVariableName(notifyType.Name)}"), false, () =>
                {
                    int durationFrames = duration ? DefaultNotifyStateDurationFrames : 0;
                    AddNotifyToTrackAtFrame(trackIndex, (AnimationNotify)Activator.CreateInstance(notifyType), durationFrames, frame);
                });
            }
        }

        private AnimationNotifyEvent AddNotifyToTrackAtFrame(int trackIndex, AnimationNotify notify, int durationFrames, int frame)
        {
            if (!CanEditSelectedAsset || !HasNotifyTrack(trackIndex))
            {
                return null;
            }

            state.SetSelectedNotifyTrackIndex(trackIndex);
            state.SetPreviewFrame(frame);
            AnimationNotifyEvent notifyEvent = eventService.AddNotify(state.EditingAsset, trackIndex, notify, state.PreviewFrame, durationFrames);
            RebuildSerializedAsset();
            return notifyEvent;
        }

        private AnimationNotifyTrack InsertNotifyTrackAfter(int trackIndex)
        {
            if (!CanEditSelectedAsset || !HasNotifyTrack(trackIndex))
            {
                return null;
            }

            AnimationNotifyTrack track = eventService.InsertTrack(state.EditingAsset, trackIndex, GetNextNotifyTrackName());
            if (track != null)
            {
                state.SetSelectedNotifyTrackIndex(trackIndex + 1);
                RebuildSerializedAsset();
            }

            return track;
        }

        private bool RemoveNotifyTrack(int trackIndex)
        {
            if (!CanEditSelectedAsset || !HasNotifyTrack(trackIndex))
            {
                return false;
            }

            bool removed = eventService.RemoveTrack(state.EditingAsset, trackIndex);
            if (removed)
            {
                state.EnsureSelectedTrackInRange();
                if (renamingTrackIndex == trackIndex)
                {
                    CancelRenameTrack();
                }
                else if (renamingTrackIndex > trackIndex)
                {
                    renamingTrackIndex--;
                }

                RebuildSerializedAsset();
            }

            return removed;
        }

        private string GetNextNotifyTrackName()
        {
            const string baseName = "Notify Track";
            if (state.EditingAsset == null)
            {
                return baseName;
            }

            int index = state.EditingAsset.NotifyTracks.Count + 1;
            string trackName = $"{baseName} {index}";
            while (state.EditingAsset.NotifyTracks.Any(track => track.Name == trackName))
            {
                index++;
                trackName = $"{baseName} {index}";
            }

            return trackName;
        }

        private void BeginRenameTrack(int trackIndex)
        {
            if (!HasNotifyTrack(trackIndex))
            {
                return;
            }

            renamingTrackIndex = trackIndex;
            renamingTrackName = state.EditingAsset.NotifyTracks[trackIndex].Name;
            EditorGUI.FocusTextInControl(GetRenameControlName(trackIndex));
            Repaint();
        }

        private void CommitRenameTrack()
        {
            if (renamingTrackIndex >= 0 && HasNotifyTrack(renamingTrackIndex))
            {
                eventService.RenameTrack(state.EditingAsset, renamingTrackIndex, renamingTrackName);
                RebuildSerializedAsset();
            }

            CancelRenameTrack();
        }

        private void CancelRenameTrack()
        {
            renamingTrackIndex = -1;
            renamingTrackName = string.Empty;
        }

        private static string GetRenameControlName(int trackIndex)
        {
            return $"NotifyTrackRename{trackIndex}";
        }

        private void ShowNotifyTypeMenu(SerializedProperty events)
        {
            var menu = new GenericMenu();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<AnimationNotify>().Where(type => !type.IsAbstract))
            {
                Type notifyType = type;
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(notifyType.Name)), false, () =>
                {
                    serializedAsset.Update();
                    AddNotify(events, notifyType);
                    serializedAsset.ApplyModifiedProperties();
                    EditorUtility.SetDirty(state.EditingAsset);
                });
            }

            menu.ShowAsContext();
        }

        private static AnimationEditorDragMode GetTimelineEventDragMode(float localX, float eventWidth, bool durationEvent)
        {
            if (!durationEvent)
            {
                return AnimationEditorDragMode.MoveEvent;
            }

            if (localX <= ResizeHandleWidth)
            {
                return AnimationEditorDragMode.ResizeStart;
            }

            if (eventWidth - localX <= ResizeHandleWidth)
            {
                return AnimationEditorDragMode.ResizeEnd;
            }

            return AnimationEditorDragMode.MoveEvent;
        }

        private static IEnumerable<Type> GetNotifyMenuTypes(bool duration)
        {
            return TypeCache.GetTypesDerivedFrom<AnimationNotify>()
                .Where(type => !type.IsAbstract)
                .Where(type => type.GetConstructor(Type.EmptyTypes) != null)
                .Where(type => typeof(AnimationDurationNotify).IsAssignableFrom(type) == duration)
                .OrderBy(type => ObjectNames.NicifyVariableName(type.Name));
        }

        private static string GetNotifyLabel(SerializedProperty notify)
        {
            return NotifyLabelUtility.GetLabel(notify.managedReferenceValue as AnimationNotify);
        }

        private static string GetNotifyLabel(AnimationNotify notify)
        {
            return NotifyLabelUtility.GetLabel(notify);
        }

    }
}
