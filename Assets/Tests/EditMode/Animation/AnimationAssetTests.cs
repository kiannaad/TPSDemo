using Animancer;
using CGame.Animation;
using CGame.Animation.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CGame.Tests
{
    public class AnimationAssetTests
    {
        [Test]
        public void ClipAsset_CreatesPlayableClipTransition()
        {
            AnimationClip clip = new AnimationClip();
            AnimationClipAsset asset = ScriptableObject.CreateInstance<AnimationClipAsset>();
            asset.TryInitialize(clip);

            ITransition transition = asset.CreateTransition();

            Assert.IsTrue(asset.IsValid);
            Assert.IsInstanceOf<ClipTransition>(transition);
            Assert.AreSame(clip, ((ClipTransition)transition).Clip);
        }

        [Test]
        public void SequenceAsset_CreatesPlayableClipTransition()
        {
            AnimationClip clip = new AnimationClip();
            AnimationClipAsset clipAsset = ScriptableObject.CreateInstance<AnimationClipAsset>();
            clipAsset.TryInitialize(clip);
            AnimationSequenceAsset asset = ScriptableObject.CreateInstance<AnimationSequenceAsset>();
            asset.Clips = new[]
            {
                new AnimationSequenceAsset.SequenceEntry
                {
                    ClipAsset = clipAsset,
                },
            };

            ITransition transition = asset.CreateTransition();

            Assert.IsTrue(asset.IsValid);
            Assert.IsInstanceOf<ClipTransitionSequence>(transition);
            Assert.AreSame(clip, ((ClipTransition)transition).Clip);
        }

        [Test]
        public void ClipAsset_DoesNotRebindAnimationClipAfterInitialization()
        {
            AnimationClip firstClip = new AnimationClip();
            AnimationClip secondClip = new AnimationClip();
            AnimationClipAsset asset = ScriptableObject.CreateInstance<AnimationClipAsset>();

            bool initialized = asset.TryInitialize(firstClip);
            bool rebound = asset.TryInitialize(secondClip);

            Assert.IsTrue(initialized);
            Assert.IsFalse(rebound);
            Assert.AreSame(firstClip, asset.AnimationClip);
        }

        [Test]
        public void SequenceAsset_CreatesTransitionSequenceFromClipAssets()
        {
            AnimationClip firstClip = new AnimationClip();
            AnimationClip secondClip = new AnimationClip();
            AnimationClipAsset firstAsset = ScriptableObject.CreateInstance<AnimationClipAsset>();
            AnimationClipAsset secondAsset = ScriptableObject.CreateInstance<AnimationClipAsset>();
            firstAsset.TryInitialize(firstClip);
            secondAsset.TryInitialize(secondClip);
            secondAsset.Speed = 1.5f;
            AnimationSequenceAsset sequence = ScriptableObject.CreateInstance<AnimationSequenceAsset>();
            sequence.Clips = new[]
            {
                new AnimationSequenceAsset.SequenceEntry
                {
                    ClipAsset = firstAsset,
                    Speed = 0.5f,
                },
                new AnimationSequenceAsset.SequenceEntry
                {
                    ClipAsset = secondAsset,
                    Speed = 2f,
                },
            };

            ClipTransitionSequence transition = sequence.CreateSequenceTransition();

            Assert.IsTrue(sequence.IsValid);
            Assert.AreSame(firstClip, transition.Clip);
            Assert.AreEqual(1, transition.Others.Length);
            Assert.AreSame(secondClip, transition.Others[0].Clip);
            Assert.AreEqual(0.5f, transition.Speed);
            Assert.AreEqual(3f, transition.Others[0].Speed);
        }

        [Test]
        public void TwoDimensionalBlend_ReferencesClipAssetsAndCreatesMixerTransition()
        {
            AnimationClip idleClip = new AnimationClip();
            AnimationClip moveClip = new AnimationClip();
            AnimationClipAsset idle = ScriptableObject.CreateInstance<AnimationClipAsset>();
            AnimationClipAsset move = ScriptableObject.CreateInstance<AnimationClipAsset>();
            idle.TryInitialize(idleClip);
            move.TryInitialize(moveClip);

            TwoDimensionalAnimationBlendAsset blend = ScriptableObject.CreateInstance<TwoDimensionalAnimationBlendAsset>();
            blend.Children = new[]
            {
                new TwoDimensionalAnimationBlendAsset.BlendChild
                {
                    ClipAsset = idle,
                    Threshold = Vector2.zero,
                },
                new TwoDimensionalAnimationBlendAsset.BlendChild
                {
                    ClipAsset = move,
                    Threshold = Vector2.up,
                },
            };

            MixerTransition2D transition = blend.CreateMixerTransition();

            Assert.IsTrue(blend.IsValid);
            Assert.AreEqual(2, transition.Animations.Length);
            Assert.AreSame(idleClip, transition.Animations[0]);
            Assert.AreSame(moveClip, transition.Animations[1]);
            Assert.AreEqual(Vector2.zero, transition.Thresholds[0]);
            Assert.AreEqual(Vector2.up, transition.Thresholds[1]);
        }

        [Test]
        public void AnimationAssetPlayer_PlaysAssetThroughAnimancerComponent()
        {
            GameObject gameObject = new GameObject("AnimationAssetPlayerTest");
            try
            {
                gameObject.AddComponent<Animator>();
                AnimancerComponent animancer = gameObject.AddComponent<AnimancerComponent>();
                AnimationAssetPlayer player = gameObject.AddComponent<AnimationAssetPlayer>();
                AnimationClipAsset asset = ScriptableObject.CreateInstance<AnimationClipAsset>();
                asset.TryInitialize(new AnimationClip());
                player.Animancer = animancer;
                player.AnimationAsset = asset;

                AnimancerState state = player.Play();

                Assert.IsNotNull(state);
                Assert.AreSame(asset.AnimationClip, state.MainObject);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AnimancerComponent_PlayAcceptsAnimationAssetDirectly()
        {
            GameObject gameObject = new GameObject("AnimancerComponentAnimationAssetExtensionTest");
            try
            {
                gameObject.AddComponent<Animator>();
                AnimancerComponent animancer = gameObject.AddComponent<AnimancerComponent>();
                AnimationClipAsset asset = ScriptableObject.CreateInstance<AnimationClipAsset>();
                asset.TryInitialize(new AnimationClip());

                AnimancerState state = animancer.Play(asset);

                Assert.IsNotNull(state);
                Assert.AreSame(asset.AnimationClip, state.MainObject);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AnimationAsset_AddsNotifyTracksAndEvents()
        {
            AnimationClipAsset asset = ScriptableObject.CreateInstance<AnimationClipAsset>();
            AnimationNotifyTrack track = asset.AddNotifyTrack("Footsteps");

            AnimationNotifyEvent instant = track.AddEvent(new AnimationInstantNotify(), 3);
            AnimationNotifyEvent duration = track.AddEvent(new AnimationDurationNotify(), 5, 8);

            Assert.AreEqual(1, asset.NotifyTracks.Count);
            Assert.AreEqual("Footsteps", asset.NotifyTracks[0].Name);
            Assert.IsFalse(instant.IsDuration);
            Assert.IsTrue(duration.IsDuration);
            Assert.AreEqual(13, duration.EndFrame);
        }

        [Test]
        public void NotifyEvent_ClampsMoveAndResizeToClipFrameRange()
        {
            AnimationNotifyEvent notifyEvent = new AnimationNotifyEvent
            {
                Notify = new AnimationDurationNotify(),
                StartFrame = 10,
                DurationFrames = 5,
            };

            notifyEvent.MoveToFrame(28, 30);
            Assert.AreEqual(28, notifyEvent.StartFrame);
            Assert.AreEqual(2, notifyEvent.DurationFrames);

            notifyEvent.ResizeStartFrame(20, 30);
            Assert.AreEqual(20, notifyEvent.StartFrame);
            Assert.AreEqual(10, notifyEvent.DurationFrames);

            notifyEvent.ResizeEndFrame(50, 30);
            Assert.AreEqual(20, notifyEvent.StartFrame);
            Assert.AreEqual(10, notifyEvent.DurationFrames);
        }

        [Test]
        public void AnimationEditorWindow_DisablesEditingWhenClipIsMissing()
        {
            AnimationClipAsset asset = ScriptableObject.CreateInstance<AnimationClipAsset>();
            AnimationEditorWindow window = AnimationEditorWindow.Open(asset);

            try
            {
                Assert.AreSame(asset, window.EditingAsset);
                Assert.IsFalse(window.CanEditSelectedAsset);

                asset.TryInitialize(new AnimationClip());

                Assert.IsTrue(window.CanEditSelectedAsset);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void OpenHandler_OpensProjectAnimationAssets()
        {
            AnimationClipAsset asset = ScriptableObject.CreateInstance<AnimationClipAsset>();
            int instanceId = asset.GetInstanceID();

            bool handled = AnimationAssetOpenHandler.OnOpenAsset(instanceId, 0);

            Assert.IsTrue(handled);
            EditorWindow.GetWindow<AnimationEditorWindow>().Close();
        }

        [Test]
        public void AnimationEditorWindow_ClampsPreviewFrameToClipLength()
        {
            AnimationClipAsset asset = ScriptableObject.CreateInstance<AnimationClipAsset>();
            AnimationClip clip = new AnimationClip
            {
                frameRate = 10f,
            };
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            asset.TryInitialize(clip);
            AnimationEditorWindow window = AnimationEditorWindow.Open(asset);

            try
            {
                window.SetPreviewFrame(500);

                Assert.AreEqual(10, window.PreviewFrame);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void AnimationAsset_ManagesNotifyTracks()
        {
            AnimationClipAsset asset = ScriptableObject.CreateInstance<AnimationClipAsset>();
            asset.AddNotifyTrack("Default");

            bool renamed = asset.RenameNotifyTrack(0, "Footstep");
            bool removed = asset.RemoveNotifyTrackAt(0);

            Assert.IsTrue(renamed);
            Assert.IsTrue(removed);
            Assert.AreEqual(0, asset.NotifyTracks.Count);
        }

        [Test]
        public void AnimationEditorWindow_AddsNotifyEventsToSelectedTrack()
        {
            AnimationClipAsset asset = CreateEditableClipAsset();
            AnimationEditorWindow window = AnimationEditorWindow.Open(asset);

            try
            {
                window.AddNotifyTrackToAsset("Footstep");
                window.AddNotifyTrackToAsset("Weapon");
                window.SelectNotifyTrack(1);
                window.SetPreviewFrame(4);

                AnimationNotifyEvent notifyEvent = window.AddNotifyToSelectedTrack();

                Assert.IsNotNull(notifyEvent);
                Assert.AreEqual(2, asset.NotifyTracks.Count);
                Assert.AreEqual(0, asset.NotifyTracks[0].Events.Count);
                Assert.AreEqual(1, asset.NotifyTracks[1].Events.Count);
                Assert.IsInstanceOf<AnimationInstantNotify>(asset.NotifyTracks[1].Events[0].Notify);
                Assert.AreEqual(4, asset.NotifyTracks[1].Events[0].StartFrame);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void AnimationEditorWindow_AddsNotifyStateWithDefaultDuration()
        {
            AnimationClipAsset asset = CreateEditableClipAsset();
            AnimationEditorWindow window = AnimationEditorWindow.Open(asset);

            try
            {
                window.AddNotifyTrackToAsset("ContextFX");
                window.SetPreviewFrame(5);

                AnimationNotifyEvent notifyEvent = window.AddNotifyStateToSelectedTrack();

                Assert.IsNotNull(notifyEvent);
                Assert.IsInstanceOf<AnimationDurationNotify>(notifyEvent.Notify);
                Assert.AreEqual(5, notifyEvent.StartFrame);
                Assert.Greater(notifyEvent.DurationFrames, 0);
                Assert.LessOrEqual(notifyEvent.EndFrame, 10);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void AnimationEditorWindow_DisablesTrackAndEventCreationWithoutClip()
        {
            AnimationClipAsset asset = ScriptableObject.CreateInstance<AnimationClipAsset>();
            AnimationEditorWindow window = AnimationEditorWindow.Open(asset);

            try
            {
                Assert.IsNull(window.AddNotifyTrackToAsset("Invalid"));
                Assert.IsNull(window.AddNotifyToSelectedTrack());
                Assert.IsNull(window.AddNotifyStateToSelectedTrack());
                Assert.AreEqual(0, asset.NotifyTracks.Count);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void AnimationEditorWindow_TimelineZoomChangesFrameCoordinateMapping()
        {
            AnimationClipAsset asset = CreateEditableClipAsset();
            AnimationEditorWindow window = AnimationEditorWindow.Open(asset);

            try
            {
                window.SetTimelineFrameWidth(8f);
                float zoomedOutX = window.FrameToTimelineXForTesting(5);
                int zoomedOutFrame = window.TimelineXToFrameForTesting(zoomedOutX, 10);

                window.SetTimelineFrameWidth(24f);
                float zoomedInX = window.FrameToTimelineXForTesting(5);
                int zoomedInFrame = window.TimelineXToFrameForTesting(zoomedInX, 10);

                Assert.AreEqual(40f, zoomedOutX);
                Assert.AreEqual(120f, zoomedInX);
                Assert.AreEqual(5, zoomedOutFrame);
                Assert.AreEqual(5, zoomedInFrame);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void AnimationEditorWindow_RemoveSelectedTrackClampsSelection()
        {
            AnimationClipAsset asset = CreateEditableClipAsset();
            AnimationEditorWindow window = AnimationEditorWindow.Open(asset);

            try
            {
                window.AddNotifyTrackToAsset("Footstep");
                window.AddNotifyTrackToAsset("Weapon");
                window.SelectNotifyTrack(1);

                bool removed = window.RemoveSelectedNotifyTrack();

                Assert.IsTrue(removed);
                Assert.AreEqual(1, asset.NotifyTracks.Count);
                Assert.AreEqual(0, window.SelectedNotifyTrackIndex);
            }
            finally
            {
                window.Close();
            }
        }

        private static AnimationClipAsset CreateEditableClipAsset()
        {
            AnimationClipAsset asset = ScriptableObject.CreateInstance<AnimationClipAsset>();
            AnimationClip clip = new AnimationClip
            {
                frameRate = 10f,
            };
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            asset.TryInitialize(clip);
            return asset;
        }
    }
}
