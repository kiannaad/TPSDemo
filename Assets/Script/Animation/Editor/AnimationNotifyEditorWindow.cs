using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CGame.Animation.Editor
{
    public class AnimationNotifyEditorWindow : EditorWindow
    {
        private const float TrackHeaderWidth = 180f;
        private const float FrameWidth = 64f;
        private const float DefaultTimelineFrameWidth = 12f;
        private const float MinTimelineFrameWidth = 1f;
        private const float MaxTimelineFrameWidth = 40f;
        private const float TimelineRulerHeight = 24f;
        private const float TrackHeight = 32f;
        private const float ResizeHandleWidth = 6f;
        private const float MinimumEventWidth = 10f;
        private const int DefaultNotifyStateDurationFrames = 8;

        [SerializeField] private AnimationAssetBase animationAsset;
        [SerializeField] private GameObject previewSource;
        [SerializeField] private int previewFrame;
        [SerializeField] private int selectedNotifyTrackIndex;
        [SerializeField] private float timelineFrameWidth = DefaultTimelineFrameWidth;

        private SerializedObject serializedAsset;
        private Vector2 scroll;
        private Vector2 timelineScroll;
        private PreviewRenderUtility previewUtility;
        private GameObject previewInstance;
        private int activeDragTrack = -1;
        private int activeDragEvent = -1;
        private DragMode activeDragMode;

        public AnimationAssetBase EditingAsset => animationAsset;
        public bool CanEditSelectedAsset => animationAsset != null && animationAsset.CanEditNotifies;
        public int PreviewFrame => previewFrame;
        public int SelectedNotifyTrackIndex => selectedNotifyTrackIndex;
        public float TimelineFrameWidth => timelineFrameWidth;

        private enum DragMode
        {
            None,
            Scrub,
            MoveEvent,
            ResizeStart,
            ResizeEnd,
        }

        [MenuItem("CGame/Animation/Notify Editor")]
        public static AnimationNotifyEditorWindow Open()
        {
            var window = GetWindow<AnimationNotifyEditorWindow>("Animation Notify");
            window.SyncFromSelection();
            return window;
        }

        public static AnimationNotifyEditorWindow Open(AnimationAssetBase asset)
        {
            var window = GetWindow<AnimationNotifyEditorWindow>("Animation Notify");
            window.SetAsset(asset);
            return window;
        }

        public void SetAsset(AnimationAssetBase asset)
        {
            animationAsset = asset;
            serializedAsset = asset == null ? null : new SerializedObject(asset);
            Repaint();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            if (animationAsset == null)
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
            if (animationAsset == null)
            {
                EditorGUILayout.HelpBox("Select an AnimationClipAsset or AnimationSequenceAsset to edit notifies.", MessageType.Info);
                return;
            }

            if (serializedAsset == null || serializedAsset.targetObject != animationAsset)
            {
                serializedAsset = new SerializedObject(animationAsset);
            }

            serializedAsset.Update();

            using (new EditorGUI.DisabledScope(!CanEditSelectedAsset))
            {
                SerializedProperty tracks = serializedAsset.FindProperty("notifyTracks");
                DrawStaticPreviewPanel();
                DrawTimeline(tracks);
            }

            serializedAsset.ApplyModifiedProperties();
        }

        public void SetPreviewFrame(int frame)
        {
            AnimationClip clip = animationAsset == null ? null : animationAsset.MainClip;
            previewFrame = Mathf.Clamp(frame, 0, GetTotalFrames(clip));
            Repaint();
        }

        public void SetTimelineFrameWidth(float frameWidth)
        {
            timelineFrameWidth = Mathf.Clamp(frameWidth, MinTimelineFrameWidth, MaxTimelineFrameWidth);
            Repaint();
        }

        public float FrameToTimelineXForTesting(int frame)
        {
            return FrameToX(Rect.zero, frame, timelineFrameWidth);
        }

        public int TimelineXToFrameForTesting(float x, int totalFrames)
        {
            return XToFrame(Rect.zero, x, totalFrames, timelineFrameWidth);
        }

        public bool SelectNotifyTrack(int trackIndex)
        {
            if (!HasNotifyTrack(trackIndex))
            {
                return false;
            }

            selectedNotifyTrackIndex = trackIndex;
            Repaint();
            return true;
        }

        public AnimationNotifyTrack AddNotifyTrackToAsset(string trackName = "Notify Track")
        {
            if (!CanEditSelectedAsset)
            {
                return null;
            }

            Undo.RecordObject(animationAsset, "Add Notify Track");
            AnimationNotifyTrack track = animationAsset.AddNotifyTrack(trackName);
            selectedNotifyTrackIndex = animationAsset.NotifyTracks.Count - 1;
            EditorUtility.SetDirty(animationAsset);
            RebuildSerializedAsset();
            return track;
        }

        public bool RenameSelectedNotifyTrack(string trackName)
        {
            if (!CanEditSelectedAsset || !HasNotifyTrack(selectedNotifyTrackIndex))
            {
                return false;
            }

            Undo.RecordObject(animationAsset, "Rename Notify Track");
            bool renamed = animationAsset.RenameNotifyTrack(selectedNotifyTrackIndex, trackName);
            EditorUtility.SetDirty(animationAsset);
            RebuildSerializedAsset();
            return renamed;
        }

        public bool RemoveSelectedNotifyTrack()
        {
            if (!CanEditSelectedAsset || !HasNotifyTrack(selectedNotifyTrackIndex))
            {
                return false;
            }

            Undo.RecordObject(animationAsset, "Remove Notify Track");
            bool removed = animationAsset.RemoveNotifyTrackAt(selectedNotifyTrackIndex);
            selectedNotifyTrackIndex = Mathf.Clamp(selectedNotifyTrackIndex, 0, Mathf.Max(0, animationAsset.NotifyTracks.Count - 1));
            EditorUtility.SetDirty(animationAsset);
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
                var selected = (AnimationAssetBase)EditorGUILayout.ObjectField(animationAsset, typeof(AnimationAssetBase), false);
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
            AnimationClip clip = animationAsset.MainClip;
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
            AnimationClip clip = animationAsset.MainClip;
            if (clip == null)
            {
                return;
            }

            float frameRate = Mathf.Max(1f, clip.frameRate);
            int maxFrame = Mathf.Max(0, Mathf.RoundToInt(clip.length * frameRate));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Pose Preview", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            previewSource = (GameObject)EditorGUILayout.ObjectField("Preview Source", previewSource, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                RebuildPreviewInstance();
            }

            SetPreviewFrame(EditorGUILayout.IntSlider("Frame", previewFrame, 0, maxFrame));

            Rect previewRect = GUILayoutUtility.GetRect(10f, 180f, GUILayout.ExpandWidth(true));
            DrawPreviewViewport(previewRect, clip, frameRate);
        }

        private void DrawStaticPreviewPanel()
        {
            AnimationClip clip = animationAsset.MainClip;
            if (clip == null)
            {
                EditorGUILayout.HelpBox("This asset has no AnimationClip. Timeline preview is disabled until a clip is assigned.", MessageType.Warning);
                return;
            }

            float frameRate = Mathf.Max(1f, clip.frameRate);
            Rect previewRect = GUILayoutUtility.GetRect(10f, Mathf.Max(180f, position.height * 0.36f), GUILayout.ExpandWidth(true));
            DrawPreviewViewport(previewRect, clip, frameRate);
            DrawStaticAssetOverlay(previewRect, clip);
        }

        private void DrawStaticAssetOverlay(Rect previewRect, AnimationClip clip)
        {
            Rect labelRect = new Rect(previewRect.x + 8f, previewRect.y + 6f, previewRect.width - 16f, 42f);
            EditorGUI.DrawRect(labelRect, new Color(0f, 0f, 0f, 0.35f));
            GUI.Label(new Rect(labelRect.x + 8f, labelRect.y + 4f, labelRect.width - 16f, 18f), animationAsset.name, EditorStyles.whiteBoldLabel);
            GUI.Label(new Rect(labelRect.x + 8f, labelRect.y + 22f, labelRect.width - 16f, 16f), $"{clip.name}  {clip.length:0.###}s  {clip.frameRate:0.##} fps", EditorStyles.whiteMiniLabel);
        }

        private void DrawPreviewViewport(Rect rect, AnimationClip clip, float frameRate)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            if (previewSource == null)
            {
                GUI.Label(rect, "Assign a Preview Source model or prefab.", EditorStyles.centeredGreyMiniLabel);
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

                using (new EditorGUI.DisabledScope(!HasNotifyTrack(selectedNotifyTrackIndex)))
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
                GUIStyle style = i == selectedNotifyTrackIndex ? EditorStyles.toolbarButton : EditorStyles.miniButton;

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Toggle(i == selectedNotifyTrackIndex, string.Empty, style, GUILayout.Width(20f)))
                    {
                        SelectNotifyTrack(i);
                    }

                    if (i == selectedNotifyTrackIndex)
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
                using (new EditorGUI.DisabledScope(!HasNotifyTrack(selectedNotifyTrackIndex)))
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

            if (!HasNotifyTrack(selectedNotifyTrackIndex))
            {
                EditorGUILayout.HelpBox("Select or create a track to add events.", MessageType.Info);
                return;
            }

            SerializedProperty selectedTrack = tracks.GetArrayElementAtIndex(selectedNotifyTrackIndex);
            SerializedProperty events = selectedTrack.FindPropertyRelative("events");
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(90f));
            DrawTrackEvents(events);
            EditorGUILayout.EndScrollView();
        }

        private void DrawTimeline(SerializedProperty tracks)
        {
            AnimationClip clip = animationAsset.MainClip;
            int totalFrames = GetTotalFrames(clip);
            float availableTimelineWidth = Mathf.Max(1f, position.width - TrackHeaderWidth - 28f);
            SetTimelineFrameWidth(CalculateFitFrameWidth(totalFrames, availableTimelineWidth));
            float timelineWidth = availableTimelineWidth;
            float timelineHeight = TimelineRulerHeight + Mathf.Max(1, tracks.arraySize) * TrackHeight;

            EditorGUILayout.Space(6f);
            Rect timelineRect = GUILayoutUtility.GetRect(TrackHeaderWidth + timelineWidth, Mathf.Min(position.height * 0.5f, timelineHeight));
            DrawTimelineBackground(timelineRect, timelineWidth, totalFrames);
            HandleScrubber(timelineRect, totalFrames);

            for (int trackIndex = 0; trackIndex < tracks.arraySize; trackIndex++)
            {
                DrawTimelineTrack(timelineRect, tracks.GetArrayElementAtIndex(trackIndex), trackIndex, totalFrames);
            }

            DrawScrubber(timelineRect, totalFrames);
        }

        private void DrawTimelineBackground(Rect timelineRect, float timelineWidth, int totalFrames)
        {
            Rect headerRect = new Rect(timelineRect.x, timelineRect.y, TrackHeaderWidth, TimelineRulerHeight);
            EditorGUI.DrawRect(headerRect, new Color(0.16f, 0.16f, 0.16f));
            GUI.Label(new Rect(headerRect.x + 6f, headerRect.y + 4f, headerRect.width - 12f, headerRect.height - 4f), "Tracks", EditorStyles.miniBoldLabel);

            Rect rulerRect = new Rect(timelineRect.x + TrackHeaderWidth, timelineRect.y, timelineWidth, TimelineRulerHeight);
            EditorGUI.DrawRect(rulerRect, new Color(0.12f, 0.12f, 0.12f));

            int majorStep = GetMajorFrameStep(totalFrames);
            for (int frame = 0; frame <= totalFrames; frame++)
            {
                float x = FrameToX(rulerRect, frame, timelineFrameWidth);
                bool major = frame % majorStep == 0 || frame == totalFrames;
                Color color = major ? new Color(0.38f, 0.38f, 0.38f) : new Color(0.22f, 0.22f, 0.22f);
                EditorGUI.DrawRect(new Rect(x, timelineRect.y, 1f, timelineRect.height), color);
                if (major)
                {
                    GUI.Label(new Rect(x + 2f, rulerRect.y + 3f, 54f, 16f), frame.ToString(), EditorStyles.miniLabel);
                }
            }
        }

        private void DrawTimelineTrack(Rect timelineRect, SerializedProperty track, int trackIndex, int totalFrames)
        {
            SerializedProperty name = track.FindPropertyRelative("name");
            SerializedProperty events = track.FindPropertyRelative("events");
            float y = timelineRect.y + TimelineRulerHeight + trackIndex * TrackHeight;
            Rect labelRect = new Rect(timelineRect.x, y, TrackHeaderWidth, TrackHeight);
            Rect laneRect = new Rect(timelineRect.x + TrackHeaderWidth, y, timelineRect.width - TrackHeaderWidth, TrackHeight);

            EditorGUI.DrawRect(labelRect, trackIndex % 2 == 0 ? new Color(0.19f, 0.19f, 0.19f) : new Color(0.16f, 0.16f, 0.16f));
            EditorGUI.DrawRect(laneRect, trackIndex % 2 == 0 ? new Color(0.11f, 0.13f, 0.14f) : new Color(0.09f, 0.11f, 0.12f));
            if (trackIndex == selectedNotifyTrackIndex)
            {
                EditorGUI.DrawRect(new Rect(labelRect.x, labelRect.y, 3f, labelRect.height), new Color(0.2f, 0.65f, 1f));
            }

            GUI.Label(new Rect(labelRect.x + 6f, labelRect.y + 7f, labelRect.width - 12f, 18f), name.stringValue, EditorStyles.miniBoldLabel);

            for (int eventIndex = 0; eventIndex < events.arraySize; eventIndex++)
            {
                DrawTimelineEvent(laneRect, events.GetArrayElementAtIndex(eventIndex), trackIndex, eventIndex, totalFrames);
            }
        }

        private void DrawTimelineEvent(Rect laneRect, SerializedProperty notifyEvent, int trackIndex, int eventIndex, int totalFrames)
        {
            SerializedProperty notify = notifyEvent.FindPropertyRelative("notify");
            SerializedProperty startFrame = notifyEvent.FindPropertyRelative("startFrame");
            SerializedProperty durationFrames = notifyEvent.FindPropertyRelative("durationFrames");
            int duration = Mathf.Max(0, durationFrames.intValue);
            float x = FrameToX(laneRect, startFrame.intValue, timelineFrameWidth);
            float width = duration == 0 ? MinimumEventWidth : Mathf.Max(MinimumEventWidth, duration * timelineFrameWidth);
            Rect eventRect = new Rect(x, laneRect.y + 6f, width, laneRect.height - 12f);
            Color eventColor = duration == 0 ? new Color(0.32f, 0.62f, 0.92f) : new Color(0.91f, 0.62f, 0.22f);

            EditorGUI.DrawRect(eventRect, eventColor);
            GUI.Label(new Rect(eventRect.x + 4f, eventRect.y + 2f, Mathf.Max(40f, eventRect.width - 8f), 16f), GetNotifyLabel(notify), EditorStyles.whiteMiniLabel);
            HandleTimelineEvent(eventRect, notifyEvent, trackIndex, eventIndex, totalFrames);
        }

        private void HandleScrubber(Rect timelineRect, int totalFrames)
        {
            Rect rulerRect = new Rect(timelineRect.x + TrackHeaderWidth, timelineRect.y, timelineRect.width - TrackHeaderWidth, TimelineRulerHeight);
            Event current = Event.current;
            if (current.type == EventType.MouseDown && rulerRect.Contains(current.mousePosition))
            {
                activeDragMode = DragMode.Scrub;
                SetPreviewFrame(XToFrame(rulerRect, current.mousePosition.x, totalFrames, timelineFrameWidth));
                current.Use();
            }

            if (activeDragMode == DragMode.Scrub && current.type == EventType.MouseDrag)
            {
                SetPreviewFrame(XToFrame(rulerRect, current.mousePosition.x, totalFrames, timelineFrameWidth));
                current.Use();
            }

            if (activeDragMode == DragMode.Scrub && current.type == EventType.MouseUp)
            {
                activeDragMode = DragMode.None;
                current.Use();
            }
        }

        private void DrawScrubber(Rect timelineRect, int totalFrames)
        {
            Rect rulerRect = new Rect(timelineRect.x + TrackHeaderWidth, timelineRect.y, timelineRect.width - TrackHeaderWidth, timelineRect.height);
            float x = FrameToX(rulerRect, previewFrame, timelineFrameWidth);
            EditorGUI.DrawRect(new Rect(x - 1f, timelineRect.y, 2f, timelineRect.height), new Color(0.95f, 0.25f, 0.2f));
            GUI.Label(new Rect(x + 4f, timelineRect.y + 2f, 72f, 18f), previewFrame.ToString(), EditorStyles.miniBoldLabel);
        }

        private void HandleTimelineEvent(Rect eventRect, SerializedProperty notifyEvent, int trackIndex, int eventIndex, int totalFrames)
        {
            Event current = Event.current;
            if (current.type == EventType.MouseDown && eventRect.Contains(current.mousePosition))
            {
                activeDragTrack = trackIndex;
                activeDragEvent = eventIndex;
                float localX = current.mousePosition.x - eventRect.x;
                if (localX <= ResizeHandleWidth)
                {
                    activeDragMode = DragMode.ResizeStart;
                }
                else if (eventRect.width - localX <= ResizeHandleWidth)
                {
                    activeDragMode = DragMode.ResizeEnd;
                }
                else
                {
                    activeDragMode = DragMode.MoveEvent;
                }

                current.Use();
            }

            if (activeDragTrack != trackIndex || activeDragEvent != eventIndex)
            {
                return;
            }

            Rect laneRect = new Rect(eventRect.xMin - notifyEvent.FindPropertyRelative("startFrame").intValue * timelineFrameWidth, eventRect.y, position.width - TrackHeaderWidth, eventRect.height);
            if (current.type == EventType.MouseDrag)
            {
                int frame = XToFrame(laneRect, current.mousePosition.x, totalFrames, timelineFrameWidth);
                ApplyEventDrag(notifyEvent, frame, totalFrames);
                current.Use();
            }

            if (current.type == EventType.MouseUp)
            {
                activeDragTrack = -1;
                activeDragEvent = -1;
                activeDragMode = DragMode.None;
                current.Use();
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

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(GetNotifyLabel(notify), GUILayout.Width(TrackHeaderWidth));
                    startFrame.intValue = Mathf.Max(0, EditorGUILayout.IntField(startFrame.intValue, GUILayout.Width(FrameWidth)));
                    durationFrames.intValue = Mathf.Max(0, EditorGUILayout.IntField(durationFrames.intValue, GUILayout.Width(FrameWidth)));
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
            notifyEvent.FindPropertyRelative("startFrame").intValue = previewFrame;
            notifyEvent.FindPropertyRelative("durationFrames").intValue = notifyType == typeof(AnimationDurationNotify) ? 1 : 0;
            notifyEvent.FindPropertyRelative("notify").managedReferenceValue = Activator.CreateInstance(notifyType);
        }

        private AnimationNotifyEvent AddNotifyToSelectedTrack(AnimationNotify notify, int durationFrames)
        {
            if (!CanEditSelectedAsset || !HasNotifyTrack(selectedNotifyTrackIndex))
            {
                return null;
            }

            Undo.RecordObject(animationAsset, "Add Notify Event");
            int maxFrame = GetTotalFrames(animationAsset.MainClip);
            int clampedDuration = Mathf.Clamp(durationFrames, 0, Mathf.Max(0, maxFrame - previewFrame));
            AnimationNotifyEvent notifyEvent = animationAsset.AddNotifyEvent(selectedNotifyTrackIndex, notify, previewFrame, clampedDuration);
            EditorUtility.SetDirty(animationAsset);
            RebuildSerializedAsset();
            return notifyEvent;
        }

        private void ApplyEventDrag(SerializedProperty notifyEvent, int frame, int totalFrames)
        {
            SerializedProperty startFrame = notifyEvent.FindPropertyRelative("startFrame");
            SerializedProperty durationFrames = notifyEvent.FindPropertyRelative("durationFrames");
            int start = startFrame.intValue;
            int duration = durationFrames.intValue;

            if (activeDragMode == DragMode.MoveEvent)
            {
                startFrame.intValue = Mathf.Clamp(frame, 0, Mathf.Max(0, totalFrames - duration));
            }
            else if (activeDragMode == DragMode.ResizeStart)
            {
                int endFrame = Mathf.Clamp(start + duration, 0, totalFrames);
                int newStart = Mathf.Clamp(frame, 0, endFrame);
                startFrame.intValue = newStart;
                durationFrames.intValue = endFrame - newStart;
            }
            else if (activeDragMode == DragMode.ResizeEnd)
            {
                durationFrames.intValue = Mathf.Clamp(frame - start, 0, totalFrames - start);
            }

            SetPreviewFrame(startFrame.intValue);
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
                    EditorUtility.SetDirty(animationAsset);
                });
            }

            menu.ShowAsContext();
        }

        private void SamplePreviewFrame(AnimationClip clip, float frameRate)
        {
            float time = Mathf.Clamp(previewFrame / frameRate, 0f, clip.length);
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
            serializedAsset = animationAsset == null ? null : new SerializedObject(animationAsset);
            Repaint();
        }

        private bool HasNotifyTrack(int trackIndex)
        {
            return animationAsset != null && trackIndex >= 0 && trackIndex < animationAsset.NotifyTracks.Count;
        }

        private void EnsureSelectedTrackInRange(SerializedProperty tracks)
        {
            selectedNotifyTrackIndex = Mathf.Clamp(selectedNotifyTrackIndex, 0, Mathf.Max(0, tracks.arraySize - 1));
        }

        private static string GetNotifyLabel(SerializedProperty notify)
        {
            object value = notify.managedReferenceValue;
            return value == null ? "Notify" : ObjectNames.NicifyVariableName(value.GetType().Name);
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
            if (previewInstance != null || previewSource == null)
            {
                return;
            }

            RebuildPreviewInstance();
        }

        private void RebuildPreviewInstance()
        {
            DestroyPreviewInstance();
            if (previewSource == null)
            {
                return;
            }

            EnsurePreviewUtility();
            previewInstance = Instantiate(previewSource);
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

        private static int GetTotalFrames(AnimationClip clip)
        {
            if (clip == null)
            {
                return 0;
            }

            return Mathf.Max(0, Mathf.RoundToInt(clip.length * Mathf.Max(1f, clip.frameRate)));
        }

        private int GetMajorFrameStep(int totalFrames)
        {
            float targetMajorSpacing = 80f;
            int framesPerMajor = Mathf.Max(1, Mathf.RoundToInt(targetMajorSpacing / Mathf.Max(1f, timelineFrameWidth)));
            return Mathf.Min(Mathf.Max(1, totalFrames), framesPerMajor);
        }

        private static float CalculateFitFrameWidth(int totalFrames, float availableWidth)
        {
            return Mathf.Clamp(availableWidth / Mathf.Max(1, totalFrames), MinTimelineFrameWidth, MaxTimelineFrameWidth);
        }

        private static float FrameToX(Rect rect, int frame, float frameWidth)
        {
            return rect.x + frame * Mathf.Max(MinTimelineFrameWidth, frameWidth);
        }

        private static int XToFrame(Rect rect, float x, int totalFrames, float frameWidth)
        {
            return Mathf.Clamp(Mathf.RoundToInt((x - rect.x) / Mathf.Max(MinTimelineFrameWidth, frameWidth)), 0, Mathf.Max(0, totalFrames));
        }
    }
}
