using UnityEditor;
using UnityEngine;

namespace CGame.Animation.Editor
{
    public class AnimationEventService
    {
        public AnimationNotifyTrack AddTrack(AnimationAssetBase asset, string trackName)
        {
            if (!CanEdit(asset))
            {
                return null;
            }

            Undo.RegisterCompleteObjectUndo(asset, "Add Notify Track");
            AnimationNotifyTrack track = asset.AddNotifyTrack(trackName);
            MarkDirty(asset);
            return track;
        }

        public AnimationNotifyTrack InsertTrack(AnimationAssetBase asset, int trackIndex, string trackName)
        {
            if (!CanEdit(asset) || !HasTrack(asset, trackIndex))
            {
                return null;
            }

            Undo.RegisterCompleteObjectUndo(asset, "Insert Notify Track");
            AnimationNotifyTrack track = asset.InsertNotifyTrack(trackIndex + 1, trackName);
            MarkDirty(asset);
            return track;
        }

        public bool RenameTrack(AnimationAssetBase asset, int trackIndex, string trackName)
        {
            if (!CanEdit(asset) || !HasTrack(asset, trackIndex))
            {
                return false;
            }

            Undo.RegisterCompleteObjectUndo(asset, "Rename Notify Track");
            bool renamed = asset.RenameNotifyTrack(trackIndex, trackName);
            MarkDirty(asset);
            return renamed;
        }

        public bool RemoveTrack(AnimationAssetBase asset, int trackIndex)
        {
            if (!CanEdit(asset) || !HasTrack(asset, trackIndex))
            {
                return false;
            }

            Undo.RegisterCompleteObjectUndo(asset, "Remove Notify Track");
            bool removed = asset.RemoveNotifyTrackAt(trackIndex);
            MarkDirty(asset);
            return removed;
        }

        public AnimationNotifyEvent AddNotify(AnimationAssetBase asset, int trackIndex, AnimationNotify notify, int startFrame, int durationFrames)
        {
            if (!CanEdit(asset) || !HasTrack(asset, trackIndex))
            {
                return null;
            }

            Undo.RegisterCompleteObjectUndo(asset, "Add Notify Event");
            int totalFrames = TimeUtility.GetTotalFrames(asset.MainClip);
            int clampedStartFrame = Mathf.Clamp(startFrame, 0, totalFrames);
            int clampedDuration = Mathf.Clamp(durationFrames, 0, Mathf.Max(0, totalFrames - clampedStartFrame));
            AnimationNotifyEvent notifyEvent = asset.AddNotifyEvent(trackIndex, notify, clampedStartFrame, clampedDuration);
            MarkDirty(asset);
            return notifyEvent;
        }

        public bool MoveEvent(AnimationAssetBase asset, AnimationNotifyEvent notifyEvent, int frame)
        {
            if (!CanEdit(asset) || notifyEvent == null)
            {
                return false;
            }

            Undo.RegisterCompleteObjectUndo(asset, "Move Notify Event");
            notifyEvent.MoveToFrame(frame, TimeUtility.GetTotalFrames(asset.MainClip));
            MarkDirty(asset);
            return true;
        }

        public bool TrimEvent(AnimationAssetBase asset, AnimationNotifyEvent notifyEvent, int frame, AnimationEditorDragMode dragMode)
        {
            if (!CanEdit(asset) || notifyEvent == null)
            {
                return false;
            }

            int totalFrames = TimeUtility.GetTotalFrames(asset.MainClip);
            Undo.RegisterCompleteObjectUndo(asset, "Trim Notify Event");
            if (dragMode == AnimationEditorDragMode.ResizeStart)
            {
                notifyEvent.ResizeStartFrame(frame, totalFrames);
            }
            else if (dragMode == AnimationEditorDragMode.ResizeEnd)
            {
                notifyEvent.ResizeEndFrame(frame, totalFrames);
            }
            else
            {
                return false;
            }

            MarkDirty(asset);
            return true;
        }

        public bool SetEventStartFrame(AnimationAssetBase asset, AnimationNotifyEvent notifyEvent, int frame)
        {
            if (!CanEdit(asset) || notifyEvent == null)
            {
                return false;
            }

            Undo.RegisterCompleteObjectUndo(asset, "Set Notify Event Start Frame");
            int totalFrames = TimeUtility.GetTotalFrames(asset.MainClip);
            if (notifyEvent.IsDuration)
            {
                notifyEvent.ResizeStartFrame(frame, totalFrames);
            }
            else
            {
                notifyEvent.MoveToFrame(frame, totalFrames);
            }

            MarkDirty(asset);
            return true;
        }

        public bool SetEventEndFrame(AnimationAssetBase asset, AnimationNotifyEvent notifyEvent, int frame)
        {
            if (!CanEdit(asset) || notifyEvent == null)
            {
                return false;
            }

            Undo.RegisterCompleteObjectUndo(asset, "Set Notify Event End Frame");
            notifyEvent.ResizeEndFrame(frame, TimeUtility.GetTotalFrames(asset.MainClip));
            MarkDirty(asset);
            return true;
        }

        public bool SetEventMinTriggerWeight(AnimationAssetBase asset, AnimationNotifyEvent notifyEvent, float minTriggerWeight)
        {
            if (!CanEdit(asset) || notifyEvent == null)
            {
                return false;
            }

            Undo.RegisterCompleteObjectUndo(asset, "Set Notify Event Trigger Weight");
            notifyEvent.MinTriggerWeight = minTriggerWeight;
            MarkDirty(asset);
            return true;
        }

        public bool RemoveEvent(AnimationAssetBase asset, int trackIndex, int eventIndex)
        {
            if (!CanEdit(asset) || !HasEvent(asset, trackIndex, eventIndex))
            {
                return false;
            }

            Undo.RegisterCompleteObjectUndo(asset, "Delete Notify Event");
            asset.NotifyTracks[trackIndex].Events.RemoveAt(eventIndex);
            MarkDirty(asset);
            return true;
        }

        private static bool CanEdit(AnimationAssetBase asset)
        {
            return asset != null && asset.CanEditNotifies;
        }

        private static bool HasTrack(AnimationAssetBase asset, int trackIndex)
        {
            return asset != null && trackIndex >= 0 && trackIndex < asset.NotifyTracks.Count;
        }

        private static bool HasEvent(AnimationAssetBase asset, int trackIndex, int eventIndex)
        {
            return HasTrack(asset, trackIndex) && eventIndex >= 0 && eventIndex < asset.NotifyTracks[trackIndex].Events.Count;
        }

        private static void MarkDirty(AnimationAssetBase asset)
        {
            EditorUtility.SetDirty(asset);
        }
    }
}
