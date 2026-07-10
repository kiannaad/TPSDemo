using Animancer;
using CGame.Animation;
using CGame.Animation.Editor;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
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
        public void SequenceAsset_DoesNotAllowDirectNotifyEditing()
        {
            AnimationClipAsset clipAsset = ScriptableObject.CreateInstance<AnimationClipAsset>();
            clipAsset.TryInitialize(new AnimationClip());
            AnimationSequenceAsset sequence = ScriptableObject.CreateInstance<AnimationSequenceAsset>();
            sequence.Clips = new[]
            {
                new AnimationSequenceAsset.SequenceEntry
                {
                    ClipAsset = clipAsset,
                },
            };

            AnimationEditorWindow window = AnimationEditorWindow.Open(sequence);

            try
            {
                Assert.IsFalse(sequence.CanEditNotifies);
                Assert.IsFalse(window.CanEditSelectedAsset);
                Assert.IsNull(window.AddNotifyTrackToAsset("Sequence Notify Track"));
                Assert.AreEqual(0, sequence.NotifyTracks.Count);
            }
            finally
            {
                window.Close();
            }
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
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AnimationClipAssetFactory_RegistersCreateMenuPath()
        {
            MethodInfo createMenuMethod = typeof(AnimationClipAssetFactory).GetMethod(nameof(AnimationClipAssetFactory.CreateFromSelectedClipInCreateMenu));
            MenuItem menuItem = createMenuMethod.GetCustomAttributes<MenuItem>().FirstOrDefault();

            Assert.IsNotNull(menuItem);
            Assert.AreEqual("Assets/Create/CGame/Animation/Animation Clip Asset", menuItem.menuItem);
        }

        [Test]
        public void AnimationClipAssetFactory_CreatesPersistentAssetBoundToSourceClip()
        {
            const string folderPath = "Assets/Temp/AnimationAssetTests";
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
            string clipPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/FactorySource.anim");
            string createdPath = null;

            try
            {
                var clip = new AnimationClip
                {
                    frameRate = 24f,
                };
                AssetDatabase.CreateAsset(clip, clipPath);

                AnimationClipAsset createdAsset = AnimationClipAssetFactory.CreateFromClip(clip);
                createdPath = AssetDatabase.GetAssetPath(createdAsset);
                AnimationClipAsset loadedAsset = AssetDatabase.LoadAssetAtPath<AnimationClipAsset>(createdPath);

                Assert.IsNotNull(loadedAsset);
                Assert.AreSame(clip, loadedAsset.AnimationClip);
                Assert.IsFalse(loadedAsset.TryInitialize(new AnimationClip()));
            }
            finally
            {
                if (!string.IsNullOrEmpty(createdPath))
                {
                    AssetDatabase.DeleteAsset(createdPath);
                }

                AssetDatabase.DeleteAsset(clipPath);
                AssetDatabase.DeleteAsset(folderPath);
            }
        }

        [Test]
        public void AnimationClipAsset_PersistsEditedNotifyData()
        {
            const string folderPath = "Assets/Temp/AnimationAssetTests";
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
            string clipPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/PersistentSource.anim");
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/PersistentClipAsset.asset");

            try
            {
                var clip = new AnimationClip
                {
                    frameRate = 30f,
                };
                clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));
                AssetDatabase.CreateAsset(clip, clipPath);

                AnimationClipAsset asset = ScriptableObject.CreateInstance<AnimationClipAsset>();
                asset.TryInitialize(clip);
                AssetDatabase.CreateAsset(asset, assetPath);
                AnimationNotifyTrack track = asset.AddNotifyTrack("Footsteps");
                AnimationNotifyEvent notifyEvent = track.AddEvent(new AnimationInstantNotify { DisplayName = "Foot Plant" }, 12);
                notifyEvent.MinTriggerWeight = 0.25f;
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();

                AnimationClipAsset loadedAsset = AssetDatabase.LoadAssetAtPath<AnimationClipAsset>(assetPath);

                Assert.IsNotNull(loadedAsset);
                Assert.AreEqual(1, loadedAsset.NotifyTracks.Count);
                Assert.AreEqual("Footsteps", loadedAsset.NotifyTracks[0].Name);
                Assert.AreEqual(1, loadedAsset.NotifyTracks[0].Events.Count);
                Assert.AreEqual(12, loadedAsset.NotifyTracks[0].Events[0].StartFrame);
                Assert.AreEqual(0.25f, loadedAsset.NotifyTracks[0].Events[0].MinTriggerWeight);
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.DeleteAsset(clipPath);
                AssetDatabase.DeleteAsset(folderPath);
            }
        }

        [Test]
        public void HumanoidSampleClipAsset_IsEditableAndPlayable()
        {
            const string sampleAssetPath = "Assets/Samples/Animancer/8.0.2/Animancer Samples/Art/Humanoid Animations/Humanoid-IdleClipAsset.asset";
            AnimationClipAsset asset = AssetDatabase.LoadAssetAtPath<AnimationClipAsset>(sampleAssetPath);
            Assert.IsNotNull(asset);
            Assert.IsTrue(asset.IsValid);
            Assert.IsTrue(asset.CanEditNotifies);

            GameObject gameObject = new GameObject("HumanoidSampleClipAssetPlaybackTest");
            try
            {
                gameObject.AddComponent<Animator>();
                AnimancerComponent animancer = gameObject.AddComponent<AnimancerComponent>();

                AnimancerState state = animancer.Play(asset);

                Assert.IsNotNull(state);
                Assert.AreSame(asset.AnimationClip, state.MainObject);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void QuickPlaySampleScript_PlaysAnimationClipAssetOnEnable()
        {
            const string sampleAssetPath = "Assets/Samples/Animancer/8.0.2/Animancer Samples/Art/Humanoid Animations/Humanoid-IdleClipAsset.asset";
            Type sampleType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("Animancer.Samples.Basics.PlayAnimationOnEnable"))
                .FirstOrDefault(type => type != null);
            AnimationClipAsset asset = AssetDatabase.LoadAssetAtPath<AnimationClipAsset>(sampleAssetPath);
            Assert.IsNotNull(sampleType);
            Assert.IsNotNull(asset);

            GameObject gameObject = new GameObject("QuickPlaySampleScriptPlaybackTest");
            try
            {
                gameObject.AddComponent<Animator>();
                AnimancerComponent animancer = gameObject.AddComponent<AnimancerComponent>();
                Component sample = gameObject.AddComponent(sampleType);
                sampleType.GetField("animation", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(sample, asset);
                sampleType.GetField("animancer", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(sample, animancer);

                sampleType.GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(sample, null);

                Assert.IsNotNull(animancer.States.Current);
                Assert.AreSame(asset.AnimationClip, animancer.States.Current.MainObject);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
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
                UnityEngine.Object.DestroyImmediate(gameObject);
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
        public void AnimationEditorWindow_AddsNotifyItemToRequestedTrackAndFrame()
        {
            AnimationClipAsset asset = CreateEditableClipAsset();
            AnimationEditorWindow window = AnimationEditorWindow.Open(asset);

            try
            {
                window.AddNotifyTrackToAsset("Footstep");
                window.AddNotifyTrackToAsset("Weapon");

                AnimationNotifyEvent notifyEvent = window.AddNotifyToTrackAtFrameForTesting(0, 7, false);

                Assert.IsNotNull(notifyEvent);
                Assert.AreEqual(1, asset.NotifyTracks[0].Events.Count);
                Assert.AreEqual(0, asset.NotifyTracks[1].Events.Count);
                Assert.AreEqual(7, asset.NotifyTracks[0].Events[0].StartFrame);
                Assert.AreEqual(0, asset.NotifyTracks[0].Events[0].DurationFrames);
                Assert.AreEqual(0, window.SelectedNotifyTrackIndex);
                Assert.AreEqual(7, window.PreviewFrame);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void AnimationEditorWindow_InsertsNotifyTrackAfterRequestedTrack()
        {
            AnimationClipAsset asset = CreateEditableClipAsset();
            AnimationEditorWindow window = AnimationEditorWindow.Open(asset);

            try
            {
                window.AddNotifyTrackToAsset("Footstep");
                window.AddNotifyTrackToAsset("Weapon");

                AnimationNotifyTrack insertedTrack = window.InsertNotifyTrackAfterForTesting(0);

                Assert.IsNotNull(insertedTrack);
                Assert.AreEqual(3, asset.NotifyTracks.Count);
                Assert.AreEqual("Footstep", asset.NotifyTracks[0].Name);
                Assert.AreSame(insertedTrack, asset.NotifyTracks[1]);
                Assert.AreEqual("Weapon", asset.NotifyTracks[2].Name);
                Assert.AreEqual(1, window.SelectedNotifyTrackIndex);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void AnimationEditorWindow_DoesNotResizeInstantNotifyEvents()
        {
            Assert.AreEqual(AnimationEditorDragMode.MoveEvent, AnimationEditorWindow.GetTimelineEventDragModeForTesting(0f, 12f, false));
            Assert.AreEqual(AnimationEditorDragMode.MoveEvent, AnimationEditorWindow.GetTimelineEventDragModeForTesting(11.9f, 12f, false));
            Assert.AreEqual(AnimationEditorDragMode.ResizeStart, AnimationEditorWindow.GetTimelineEventDragModeForTesting(0f, 24f, true));
            Assert.AreEqual(AnimationEditorDragMode.ResizeEnd, AnimationEditorWindow.GetTimelineEventDragModeForTesting(23.9f, 24f, true));
        }

        [Test]
        public void AnimationEditorWindow_SplitsNotifyMenuTypesByDuration()
        {
            var notifyTypes = AnimationEditorWindow.GetNotifyMenuTypesForTesting(false);
            var notifyStateTypes = AnimationEditorWindow.GetNotifyMenuTypesForTesting(true);

            Assert.Contains(typeof(AnimationInstantNotify), notifyTypes);
            Assert.IsFalse(notifyTypes.Contains(typeof(AnimationDurationNotify)));
            Assert.Contains(typeof(AnimationDurationNotify), notifyStateTypes);
            Assert.IsFalse(notifyStateTypes.Contains(typeof(AnimationInstantNotify)));
        }

        [Test]
        public void AnimationEditorWindow_UsesCustomStringFieldForNotifyLabel()
        {
            var notify = new LabelledInstantNotify();

            string label = AnimationEditorWindow.GetNotifyLabelForTesting(notify);

            Assert.AreEqual("Foot Plant Left", label);
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
        public void AnimationWindowState_StoresEditingContextAndClampsPreviewFrame()
        {
            AnimationClipAsset asset = CreateEditableClipAsset();
            asset.AddNotifyTrack("Default");
            var state = new AnimationWindowState();

            state.SetEditingAsset(asset);
            state.SetPreviewFrame(500);
            state.SetSelectedNotifyTrackIndex(20);
            state.BeginDrag(0, 1, AnimationEditorDragMode.MoveEvent);

            Assert.AreSame(asset, state.EditingAsset);
            Assert.AreEqual(10, state.PreviewFrame);
            Assert.AreEqual(0, state.SelectedNotifyTrackIndex);
            Assert.IsTrue(state.IsDragging);
            Assert.AreEqual(AnimationEditorDragMode.MoveEvent, state.ActiveDragMode);

            state.EndDrag();

            Assert.IsFalse(state.IsDragging);
        }

        [Test]
        public void TimeUtility_ConvertsBetweenFramesTimeAndPixels()
        {
            Rect rect = new Rect(12f, 0f, 200f, 20f);

            float x = TimeUtility.FrameToX(rect, 5, 8f);
            int frame = TimeUtility.XToFrame(rect, x, 10, 8f);
            double time = TimeUtility.FrameToTime(12, 24f);
            int roundTripFrame = TimeUtility.TimeToFrame(time, 24f);

            Assert.AreEqual(52f, x);
            Assert.AreEqual(5, frame);
            Assert.AreEqual(0.5d, time);
            Assert.AreEqual(12, roundTripFrame);
        }

        [Test]
        public void AnimationEventService_AddsMovesAndTrimsEvents()
        {
            AnimationClipAsset asset = CreateEditableClipAsset();
            var service = new AnimationEventService();
            service.AddTrack(asset, "Default");

            AnimationNotifyEvent notifyEvent = service.AddNotify(asset, 0, new AnimationDurationNotify(), 3, 4);
            bool moved = service.MoveEvent(asset, notifyEvent, 8);
            bool trimmed = service.TrimEvent(asset, notifyEvent, 10, AnimationEditorDragMode.ResizeEnd);

            Assert.IsNotNull(notifyEvent);
            Assert.IsTrue(moved);
            Assert.IsTrue(trimmed);
            Assert.AreEqual(8, notifyEvent.StartFrame);
            Assert.AreEqual(2, notifyEvent.DurationFrames);
        }

        [Test]
        public void AnimationEventService_EditsTriggerWeightAndDeletesEvents()
        {
            AnimationClipAsset asset = CreateEditableClipAsset();
            var service = new AnimationEventService();
            service.AddTrack(asset, "Default");
            AnimationNotifyEvent notifyEvent = service.AddNotify(asset, 0, new AnimationInstantNotify(), 3, 0);

            bool weighted = service.SetEventMinTriggerWeight(asset, notifyEvent, 0.5f);
            bool moved = service.SetEventStartFrame(asset, notifyEvent, 8);
            bool deleted = service.RemoveEvent(asset, 0, 0);

            Assert.IsTrue(weighted);
            Assert.AreEqual(0.5f, notifyEvent.MinTriggerWeight);
            Assert.IsTrue(moved);
            Assert.AreEqual(8, notifyEvent.StartFrame);
            Assert.IsTrue(deleted);
            Assert.AreEqual(0, asset.NotifyTracks[0].Events.Count);
        }

        [Test]
        public void AnimationEventService_AddTrackSupportsUndoRedo()
        {
            AnimationClipAsset asset = CreateEditableClipAsset();
            var service = new AnimationEventService();

            try
            {
                service.AddTrack(asset, "Context Track");

                Assert.AreEqual(1, asset.NotifyTracks.Count);

                Undo.PerformUndo();

                Assert.AreEqual(0, asset.NotifyTracks.Count);

                Undo.PerformRedo();

                Assert.AreEqual(1, asset.NotifyTracks.Count);
                Assert.AreEqual("Context Track", asset.NotifyTracks[0].Name);
            }
            finally
            {
                Undo.ClearUndo(asset);
            }
        }

        [Test]
        public void EventLayoutUtility_AssignsSeparateLanesForOverlappingDurationEvents()
        {
            var events = new[]
            {
                new AnimationNotifyEvent
                {
                    Notify = new AnimationDurationNotify(),
                    StartFrame = 1,
                    DurationFrames = 5,
                },
                new AnimationNotifyEvent
                {
                    Notify = new AnimationDurationNotify(),
                    StartFrame = 3,
                    DurationFrames = 5,
                },
            };

            var layouts = EventLayoutUtility.LayoutEvents(events, new Rect(0f, 0f, 200f, 32f), 10f);

            Assert.AreEqual(0, layouts[0].Lane);
            Assert.AreEqual(1, layouts[1].Lane);
            Assert.AreNotEqual(layouts[0].Rect.y, layouts[1].Rect.y);
        }

        [Test]
        public void EventLayoutUtility_SeparatesInstantEventsOnSameFrame()
        {
            var events = new[]
            {
                new AnimationNotifyEvent
                {
                    Notify = new AnimationInstantNotify(),
                    StartFrame = 2,
                },
                new AnimationNotifyEvent
                {
                    Notify = new AnimationInstantNotify(),
                    StartFrame = 2,
                },
            };

            var layouts = EventLayoutUtility.LayoutEvents(events, new Rect(0f, 0f, 200f, 38f), 10f);

            Assert.AreEqual(0, layouts[0].Lane);
            Assert.AreEqual(1, layouts[1].Lane);
            Assert.AreNotEqual(layouts[0].Rect.y, layouts[1].Rect.y);
        }

        [Test]
        public void EventLayoutUtility_AvoidsVisibleInstantEventLabelOverlap()
        {
            var events = new[]
            {
                new AnimationNotifyEvent
                {
                    Notify = new AnimationInstantNotify(),
                    StartFrame = 12,
                },
                new AnimationNotifyEvent
                {
                    Notify = new AnimationDurationNotify(),
                    StartFrame = 24,
                    DurationFrames = 8,
                },
            };

            var layouts = EventLayoutUtility.LayoutEvents(events, new Rect(0f, 0f, 200f, 38f), 4f, notifyEvent => 80f);

            Assert.AreEqual(0, layouts[0].Lane);
            Assert.AreEqual(1, layouts[1].Lane);
            Assert.AreNotEqual(layouts[0].Rect.y, layouts[1].Rect.y);
        }

        [Test]
        public void TimelineLayoutUtility_AlignsRulerContentAndTrackRows()
        {
            TimelineLayout layout = TimelineLayoutUtility.Calculate(new Rect(0f, 0f, 1000f, 720f), 2, 100);
            TrackLayout groupTrack = TimelineLayoutUtility.GetNotifyGroupLayout(layout);
            TrackLayout firstTrack = TimelineLayoutUtility.GetTrackLayout(layout, 0);

            Assert.AreEqual(layout.RulerRect.x, layout.ContentRect.x);
            Assert.AreEqual(layout.RulerRect.width, layout.ContentRect.width);
            Assert.AreEqual(layout.HeaderRect.xMax, layout.RulerRect.x);
            Assert.AreEqual(layout.ContentRect.y, groupTrack.LaneRect.y);
            Assert.AreEqual(groupTrack.RowRect.yMax + AnimationEditorConstants.TrackGap, firstTrack.LaneRect.y);
            Assert.AreEqual(layout.HeaderRect.x, firstTrack.HeaderRect.x);
            Assert.AreEqual(layout.RulerRect.x, firstTrack.LaneRect.x);
        }

        [Test]
        public void AnimationEditorWindow_TogglesNotifyTrackTreeExpansionWithoutChangingAsset()
        {
            AnimationClipAsset asset = CreateEditableClipAsset();
            AnimationEditorWindow window = AnimationEditorWindow.Open(asset);

            try
            {
                window.AddNotifyTrackToAsset("Footstep");

                Assert.IsTrue(window.NotifyTracksExpanded);

                window.SetNotifyTracksExpanded(false);

                Assert.IsFalse(window.NotifyTracksExpanded);
                Assert.AreEqual(1, asset.NotifyTracks.Count);

                window.SetNotifyTracksExpanded(true);

                Assert.IsTrue(window.NotifyTracksExpanded);
                Assert.AreEqual(1, asset.NotifyTracks.Count);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void TimelineLayoutUtility_ConservesPreviewHeightAndFillsRemainingTimelineArea()
        {
            TimelineLayout layout = TimelineLayoutUtility.Calculate(new Rect(0f, 0f, 1180f, 760f), 2, 177);

            Assert.GreaterOrEqual(layout.PreviewRect.height, AnimationEditorConstants.MinPreviewHeight);
            Assert.LessOrEqual(layout.PreviewRect.height, AnimationEditorConstants.MaxPreviewHeight);
            Assert.AreEqual(layout.RootRect.yMax, layout.TimelineRect.yMax);
            Assert.Greater(layout.TimelineRect.height, AnimationEditorConstants.TimelineHeaderHeight + AnimationEditorConstants.TimelineRulerHeight + AnimationEditorConstants.TrackHeight * 2f);
        }

        [Test]
        public void TimelineRulerUtility_AdaptsMinorTickStyleToFrameWidth()
        {
            Assert.AreEqual(TimelineRulerTickStyle.Hidden, TimelineRulerUtility.GetMinorTickStyle(AnimationEditorConstants.MinorTickDotFrameWidth - 0.1f));
            Assert.AreEqual(TimelineRulerTickStyle.Dot, TimelineRulerUtility.GetMinorTickStyle(AnimationEditorConstants.MinorTickDotFrameWidth));
            Assert.AreEqual(TimelineRulerTickStyle.Bar, TimelineRulerUtility.GetMinorTickStyle(AnimationEditorConstants.MinorTickBarFrameWidth));
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

        private class LabelledInstantNotify : AnimationInstantNotify
        {
            [SerializeField] private string eventName = "Foot Plant Left";
        }
    }
}
