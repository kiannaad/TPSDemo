using System;
using UnityEngine;

namespace CGame.Animation.Editor
{
    [Serializable]
    public class AnimationWindowState
    {
        [SerializeField] private AnimationAssetBase editingAsset;
        [SerializeField] private GameObject previewSource;
        [SerializeField] private int previewFrame;
        [SerializeField] private int selectedNotifyTrackIndex;
        [SerializeField] private float timelineFrameWidth = AnimationEditorConstants.DefaultTimelineFrameWidth;
        [SerializeField] private int activeDragTrack = -1;
        [SerializeField] private int activeDragEvent = -1;
        [SerializeField] private int selectedNotifyEventTrack = -1;
        [SerializeField] private int selectedNotifyEventIndex = -1;
        [SerializeField] private AnimationEditorDragMode activeDragMode;
        [SerializeField] private bool notifyTracksExpanded = true;
        [SerializeField] private bool repaintRequested;
        [SerializeField] private bool previewRebuildRequested;

        public AnimationAssetBase EditingAsset
        {
            get => editingAsset;
            private set => editingAsset = value;
        }

        public GameObject PreviewSource
        {
            get => previewSource;
            set
            {
                if (previewSource == value)
                {
                    return;
                }

                previewSource = value;
                RequestPreviewRebuild();
            }
        }

        public int PreviewFrame => previewFrame;
        public int SelectedNotifyTrackIndex => selectedNotifyTrackIndex;
        public float TimelineFrameWidth => timelineFrameWidth;
        public int ActiveDragTrack => activeDragTrack;
        public int ActiveDragEvent => activeDragEvent;
        public int SelectedNotifyEventTrack => selectedNotifyEventTrack;
        public int SelectedNotifyEventIndex => selectedNotifyEventIndex;
        public AnimationEditorDragMode ActiveDragMode => activeDragMode;
        public bool NotifyTracksExpanded => notifyTracksExpanded;
        public bool IsDragging => activeDragMode != AnimationEditorDragMode.None;
        public bool RepaintRequested => repaintRequested;
        public bool PreviewRebuildRequested => previewRebuildRequested;

        public void SetEditingAsset(AnimationAssetBase asset)
        {
            if (editingAsset == asset)
            {
                return;
            }

            editingAsset = asset;
            EnsureSelectedTrackInRange();
            SetPreviewFrame(previewFrame);
            RequestRepaint();
        }

        public void SetPreviewFrame(int frame)
        {
            int totalFrames = TimeUtility.GetTotalFrames(editingAsset == null ? null : editingAsset.MainClip);
            previewFrame = Mathf.Clamp(frame, 0, totalFrames);
            RequestRepaint();
        }

        public void SetSelectedNotifyTrackIndex(int trackIndex)
        {
            selectedNotifyTrackIndex = trackIndex;
            EnsureSelectedTrackInRange();
            ClearSelectedNotifyEventIfTrackChanged();
            RequestRepaint();
        }

        public void SetSelectedNotifyEvent(int trackIndex, int eventIndex)
        {
            selectedNotifyEventTrack = trackIndex;
            selectedNotifyEventIndex = eventIndex;
            RequestRepaint();
        }

        public void ClearSelectedNotifyEvent()
        {
            selectedNotifyEventTrack = -1;
            selectedNotifyEventIndex = -1;
            RequestRepaint();
        }

        public void SetTimelineFrameWidth(float frameWidth)
        {
            timelineFrameWidth = Mathf.Clamp(frameWidth, AnimationEditorConstants.MinTimelineFrameWidth, AnimationEditorConstants.MaxTimelineFrameWidth);
            RequestRepaint();
        }

        public void SetNotifyTracksExpanded(bool expanded)
        {
            if (notifyTracksExpanded == expanded)
            {
                return;
            }

            notifyTracksExpanded = expanded;
            RequestRepaint();
        }

        public void ToggleNotifyTracksExpanded()
        {
            SetNotifyTracksExpanded(!notifyTracksExpanded);
        }

        public void BeginDrag(int trackIndex, int eventIndex, AnimationEditorDragMode dragMode)
        {
            activeDragTrack = trackIndex;
            activeDragEvent = eventIndex;
            activeDragMode = dragMode;
            RequestRepaint();
        }

        public void EndDrag()
        {
            activeDragTrack = -1;
            activeDragEvent = -1;
            activeDragMode = AnimationEditorDragMode.None;
            RequestRepaint();
        }

        public void EnsureSelectedTrackInRange()
        {
            int trackCount = editingAsset == null ? 0 : editingAsset.NotifyTracks.Count;
            selectedNotifyTrackIndex = Mathf.Clamp(selectedNotifyTrackIndex, 0, Mathf.Max(0, trackCount - 1));
            if (selectedNotifyEventTrack < 0 || selectedNotifyEventTrack >= trackCount)
            {
                ClearSelectedNotifyEvent();
                return;
            }

            int eventCount = editingAsset.NotifyTracks[selectedNotifyEventTrack].Events.Count;
            if (selectedNotifyEventIndex < 0 || selectedNotifyEventIndex >= eventCount)
            {
                ClearSelectedNotifyEvent();
            }
        }

        private void ClearSelectedNotifyEventIfTrackChanged()
        {
            if (selectedNotifyEventTrack != selectedNotifyTrackIndex)
            {
                ClearSelectedNotifyEvent();
            }
        }

        public void RequestRepaint()
        {
            repaintRequested = true;
        }

        public void RequestPreviewRebuild()
        {
            previewRebuildRequested = true;
            RequestRepaint();
        }

        public void ClearRepaintRequest()
        {
            repaintRequested = false;
        }

        public void ClearPreviewRebuildRequest()
        {
            previewRebuildRequested = false;
        }
    }
}
