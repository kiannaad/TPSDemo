using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static CGame.Animation.Editor.AnimationEditorConstants;

namespace CGame.Animation.Editor
{
    public class AnimationEditorWindow : EditorWindow
    {
        private const string DefaultPreviewSourcePath = "Packages/com.kybernetik.animancer/Art/Animancer Humanoid/AnimancerHumanoid.prefab";

        [SerializeField] private AnimationWindowState state = new AnimationWindowState();
        [SerializeField] private bool eventContextPanelOpen;
        [SerializeField] private Rect eventContextPanelRect;

        private SerializedObject serializedAsset;
        private Vector2 scroll;
        private PreviewRenderUtility previewUtility;
        private GameObject previewInstance;
        private readonly AnimationEventService eventService = new AnimationEventService();
        private int renamingTrackIndex = -1;
        private string renamingTrackName = string.Empty;

        public AnimationAssetBase EditingAsset => state.EditingAsset;
        public bool CanEditSelectedAsset => state.EditingAsset != null && state.EditingAsset.CanEditNotifies;
        public int PreviewFrame => state.PreviewFrame;
        public int SelectedNotifyTrackIndex => state.SelectedNotifyTrackIndex;
        public float TimelineFrameWidth => state.TimelineFrameWidth;
        public bool NotifyTracksExpanded => state.NotifyTracksExpanded;

        public static AnimationEditorDragMode GetTimelineEventDragModeForTesting(float localX, float eventWidth, bool durationEvent)
        {
            return GetTimelineEventDragMode(localX, eventWidth, durationEvent);
        }

        public static Type[] GetNotifyMenuTypesForTesting(bool duration)
        {
            return GetNotifyMenuTypes(duration).ToArray();
        }

        public static string GetNotifyLabelForTesting(AnimationNotify notify)
        {
            return NotifyLabelUtility.GetLabel(notify);
        }

        [MenuItem("CGame/Animation/Animation Editor")]
        public static AnimationEditorWindow Open()
        {
            var window = GetWindow<AnimationEditorWindow>("Animation Editor");
            window.SyncFromSelection();
            return window;
        }

        public static AnimationEditorWindow Open(AnimationAssetBase asset)
        {
            var window = GetWindow<AnimationEditorWindow>("Animation Editor");
            window.SetAsset(asset);
            return window;
        }

        public void SetAsset(AnimationAssetBase asset)
        {
            state.SetEditingAsset(asset);
            serializedAsset = asset == null ? null : new SerializedObject(asset);
            Repaint();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            if (state.EditingAsset == null)
            {
                SyncFromSelection();
            }
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            DestroyPreviewInstance();
            previewUtility?.Cleanup();
            previewUtility = null;
        }

        private void OnSelectionChanged()
        {
            SyncFromSelection();
            Repaint();
        }

        private void OnGUI()
        {
            if (state.EditingAsset == null)
            {
                EditorGUILayout.HelpBox("Select an AnimationClipAsset or AnimationSequenceAsset to edit notifies.", MessageType.Info);
                return;
            }

            if (serializedAsset == null || serializedAsset.targetObject != state.EditingAsset)
            {
                serializedAsset = new SerializedObject(state.EditingAsset);
            }

            serializedAsset.Update();
            HandleSelectedEventKeyboard();

            SerializedProperty tracks = serializedAsset.FindProperty("notifyTracks");
            DrawEditorSurface(tracks);

            DrawEventContextPanel();

            serializedAsset.ApplyModifiedProperties();
        }

        public void SetPreviewFrame(int frame)
        {
            state.SetPreviewFrame(frame);
            Repaint();
        }

        public void SetTimelineFrameWidth(float frameWidth)
        {
            state.SetTimelineFrameWidth(frameWidth);
            Repaint();
        }

        public void SetNotifyTracksExpanded(bool expanded)
        {
            state.SetNotifyTracksExpanded(expanded);
            Repaint();
        }

        public AnimationNotifyEvent AddNotifyToTrackAtFrameForTesting(int trackIndex, int frame, bool durationEvent)
        {
            return AddNotifyToTrackAtFrame(trackIndex, durationEvent ? new AnimationDurationNotify() : new AnimationInstantNotify(), durationEvent ? DefaultNotifyStateDurationFrames : 0, frame);
        }

        public float FrameToTimelineXForTesting(int frame)
        {
            return TimeUtility.FrameToX(Rect.zero, frame, state.TimelineFrameWidth);
        }

        public int TimelineXToFrameForTesting(float x, int totalFrames)
        {
            return TimeUtility.XToFrame(Rect.zero, x, totalFrames, state.TimelineFrameWidth);
        }

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

        public AnimationNotifyTrack InsertNotifyTrackAfterForTesting(int trackIndex)
        {
            return InsertNotifyTrackAfter(trackIndex);
        }

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

        private void DrawPreviewPanel()
        {
            AnimationClip clip = state.EditingAsset.MainClip;
            if (clip == null)
            {
                return;
            }

            float frameRate = Mathf.Max(1f, clip.frameRate);
            int maxFrame = Mathf.Max(0, Mathf.RoundToInt(clip.length * frameRate));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Pose Preview", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            state.PreviewSource = (GameObject)EditorGUILayout.ObjectField("Preview Source", state.PreviewSource, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                RebuildPreviewInstance();
            }

            SetPreviewFrame(EditorGUILayout.IntSlider("Frame", state.PreviewFrame, 0, maxFrame));

            Rect previewRect = GUILayoutUtility.GetRect(10f, 180f, GUILayout.ExpandWidth(true));
            DrawPreviewViewport(previewRect, clip, frameRate);
        }

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

        private void DrawStaticPreviewPanel(Rect previewRect)
        {
            AnimationClip clip = state.EditingAsset.MainClip;
            if (clip == null)
            {
                EditorGUILayout.HelpBox("This asset has no AnimationClip. Timeline preview is disabled until a clip is assigned.", MessageType.Warning);
                return;
            }

            float frameRate = Mathf.Max(1f, clip.frameRate);
            DrawPreviewViewport(previewRect, clip, frameRate);
            DrawStaticAssetOverlay(previewRect, clip);
        }

        private void DrawStaticAssetOverlay(Rect previewRect, AnimationClip clip)
        {
            Rect labelRect = new Rect(previewRect.x + 8f, previewRect.y + 6f, previewRect.width - 16f, 42f);
            EditorGUI.DrawRect(labelRect, new Color(0f, 0f, 0f, 0.35f));
            GUI.Label(new Rect(labelRect.x + 8f, labelRect.y + 4f, labelRect.width - 16f, 18f), state.EditingAsset.name, EditorStyles.whiteBoldLabel);
            GUI.Label(new Rect(labelRect.x + 8f, labelRect.y + 22f, labelRect.width - 16f, 16f), $"{clip.name}  {clip.length:0.###}s  {clip.frameRate:0.##} fps", EditorStyles.whiteMiniLabel);
        }

        private void DrawPreviewViewport(Rect rect, AnimationClip clip, float frameRate)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            EnsurePreviewSource();
            if (state.PreviewSource == null)
            {
                GUI.Label(rect, "Preview source model could not be loaded.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            EnsurePreviewUtility();
            EnsurePreviewInstance();
            if (previewInstance == null)
            {
                GUI.Label(rect, "Preview source could not be instantiated.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            SamplePreviewFrame(clip, frameRate);
            Bounds bounds = CalculatePreviewBounds(previewInstance);
            Vector3 center = bounds.center;
            float radius = Mathf.Max(0.5f, bounds.extents.magnitude);
            previewUtility.camera.transform.position = center + new Vector3(0f, radius * 0.55f, -radius * 2.4f);
            previewUtility.camera.transform.LookAt(center + Vector3.up * radius * 0.2f);
            previewUtility.camera.nearClipPlane = 0.01f;
            previewUtility.camera.farClipPlane = radius * 8f;
            previewUtility.lights[0].intensity = 1.2f;
            previewUtility.lights[0].transform.rotation = Quaternion.Euler(35f, 35f, 0f);
            previewUtility.lights[1].intensity = 0.7f;

            previewUtility.BeginPreview(rect, GUIStyle.none);
            previewUtility.Render();
            Texture texture = previewUtility.EndPreview();
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
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

        private void DrawScrubber(TimelineLayout layout)
        {
            Rect cursorRect = new Rect(layout.RulerRect.x, layout.RulerRect.y, layout.RulerRect.width, layout.ContentRect.yMax - layout.RulerRect.y);
            float x = TimeUtility.FrameToX(cursorRect, state.PreviewFrame, state.TimelineFrameWidth);
            EditorGUI.DrawRect(new Rect(x - 1f, cursorRect.y, 2f, cursorRect.height), new Color(0.95f, 0.25f, 0.2f));
            GUI.Label(new Rect(x + 4f, layout.RulerRect.y + 2f, 72f, 18f), state.PreviewFrame.ToString(), EditorStyles.miniBoldLabel);
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

        private void SamplePreviewFrame(AnimationClip clip, float frameRate)
        {
            float time = Mathf.Clamp(state.PreviewFrame / frameRate, 0f, clip.length);
            AnimationMode.StartAnimationMode();
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(previewInstance, clip, time);
            AnimationMode.EndSampling();
        }

        private void SyncFromSelection()
        {
            if (Selection.activeObject is AnimationAssetBase selectedAsset)
            {
                SetAsset(selectedAsset);
            }
        }

        private void RebuildSerializedAsset()
        {
            serializedAsset = state.EditingAsset == null ? null : new SerializedObject(state.EditingAsset);
            Repaint();
        }

        private bool HasNotifyTrack(int trackIndex)
        {
            return state.EditingAsset != null && trackIndex >= 0 && trackIndex < state.EditingAsset.NotifyTracks.Count;
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

        private AnimationNotifyEvent GetNotifyEvent(int trackIndex, int eventIndex)
        {
            if (!HasNotifyTrack(trackIndex))
            {
                return null;
            }

            AnimationNotifyTrack track = state.EditingAsset.NotifyTracks[trackIndex];
            return eventIndex >= 0 && eventIndex < track.Events.Count ? track.Events[eventIndex] : null;
        }

        private AnimationNotifyEvent GetSelectedNotifyEvent()
        {
            return GetNotifyEvent(state.SelectedNotifyEventTrack, state.SelectedNotifyEventIndex);
        }

        private void EnsureSelectedTrackInRange(SerializedProperty tracks)
        {
            state.EnsureSelectedTrackInRange();
        }

        private static string GetNotifyLabel(SerializedProperty notify)
        {
            return NotifyLabelUtility.GetLabel(notify.managedReferenceValue as AnimationNotify);
        }

        private static string GetNotifyLabel(AnimationNotify notify)
        {
            return NotifyLabelUtility.GetLabel(notify);
        }

        private void EnsurePreviewSource()
        {
            if (state.PreviewSource != null)
            {
                return;
            }

            GameObject defaultPreviewSource = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultPreviewSourcePath);
            if (defaultPreviewSource == null)
            {
                return;
            }

            state.PreviewSource = defaultPreviewSource;
        }

        private void EnsurePreviewUtility()
        {
            if (previewUtility != null)
            {
                return;
            }

            previewUtility = new PreviewRenderUtility();
            previewUtility.cameraFieldOfView = 30f;
        }

        private void EnsurePreviewInstance()
        {
            if (previewInstance != null || state.PreviewSource == null)
            {
                return;
            }

            RebuildPreviewInstance();
        }

        private void RebuildPreviewInstance()
        {
            DestroyPreviewInstance();
            if (state.PreviewSource == null)
            {
                return;
            }

            EnsurePreviewUtility();
            previewInstance = Instantiate(state.PreviewSource);
            previewInstance.hideFlags = HideFlags.HideAndDontSave;
            previewUtility.AddSingleGO(previewInstance);
        }

        private void DestroyPreviewInstance()
        {
            if (previewInstance == null)
            {
                return;
            }

            DestroyImmediate(previewInstance);
            previewInstance = null;
        }

        private static Bounds CalculatePreviewBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

    }
}
