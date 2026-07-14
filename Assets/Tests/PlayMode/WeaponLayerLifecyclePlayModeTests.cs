using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CGame.Animation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CGame.Tests
{
    public sealed class WeaponLayerLifecyclePlayModeTests
    {
        [UnityTest]
        public IEnumerator DelayedPresentation_DropsMissedActionAndStaleGeneration()
        {
            CreateFixture(out GameObject character, out CharacterAnimationGraph graph, out CharacterWeaponAnimationBridge bridge,
                out WeaponRuntime runtime, out ControlledLoader loader, out WeaponAnimationDefinition rifleDefinition);
            try
            {
                runtime.RequestEquip(new WeaponId("rifle"));
                bridge.Update(runtime.Snapshot, 0f, 0f, 0f);
                Assert.AreEqual(WeaponBindingState.PendingPresentation, bridge.BindingState);
                runtime.RequestFire(out WeaponActionFact missedAction);
                Assert.IsFalse(graph.FireAction.IsActive);

                runtime.RequestUnequip();
                bridge.Update(runtime.Snapshot, 0f, 0f, 0f);
                runtime.RequestEquip(new WeaponId("rifle"));
                bridge.Update(runtime.Snapshot, 0f, 0f, 0f);
                Assert.AreEqual(2, loader.Requests.Count);

                FakeLease staleLease = loader.CompleteSuccess(0, rifleDefinition);
                Assert.IsTrue(staleLease.IsReleased);
                Assert.IsNull(bridge.CurrentPresentation);

                FakeLease currentLease = loader.CompleteSuccess(1, rifleDefinition);
                yield return Tick(graph, bridge, runtime, 16);
                Assert.AreEqual(WeaponBindingState.Active, bridge.BindingState);
                Assert.IsNotNull(bridge.CurrentPresentation);
                Assert.IsFalse(currentLease.IsReleased);
                Assert.IsFalse(graph.FireAction.IsActive);
                Assert.AreNotEqual(missedAction.ActionId, graph.FireAction.RequestId);
                Assert.IsFalse(bridge.CurrentPresentation.ModelActionPlayer.IsPlaying);

                bridge.Dispose();
                bridge.Dispose();
                Assert.IsTrue(currentLease.IsReleased);
                Assert.AreEqual(WeaponBindingState.Disposed, bridge.BindingState);
                yield return null;
                Assert.IsTrue(bridge.CurrentPresentation == null);
            }
            finally
            {
                bridge.Dispose();
                graph.Dispose();
                UnityEngine.Object.Destroy(character);
            }
        }

        [UnityTest]
        public IEnumerator BuildFailure_KeepsEquipmentFactAndFallsBackWithoutGhostPresentation()
        {
            CreateFixture(out GameObject character, out CharacterAnimationGraph graph, out CharacterWeaponAnimationBridge bridge,
                out WeaponRuntime runtime, out ControlledLoader loader, out _);
            try
            {
                runtime.RequestEquip(new WeaponId("rifle"));
                bridge.Update(runtime.Snapshot, 0f, 0f, 0f);
                FakeLease failedLease = loader.CompleteFailure(0, "CoreClip");
                yield return Tick(graph, bridge, runtime, 16);

                Assert.IsTrue(runtime.Snapshot.IsEquipped, "presentation failure must not roll back gameplay equipment");
                Assert.AreEqual(new WeaponId("rifle"), runtime.Snapshot.EquippedWeaponId);
                Assert.AreEqual(WeaponBindingState.Fallback, bridge.BindingState);
                Assert.IsTrue(failedLease.IsReleased);
                Assert.IsNull(bridge.CurrentPresentation);
                Assert.IsNull(graph.WeaponLayerBlend.CurrentLayer);
                Assert.IsTrue(HasDiagnostic(graph, "weaponId=rifle"));
                Assert.IsTrue(HasDiagnostic(graph, "definitionId=test-definition"));
                Assert.IsTrue(HasDiagnostic(graph, $"generation={runtime.Snapshot.Generation}"));
                Assert.IsTrue(HasDiagnostic(graph, "missingField=CoreClip"));
            }
            finally
            {
                bridge.Dispose();
                graph.Dispose();
                UnityEngine.Object.Destroy(character);
            }
        }

        [UnityTest]
        public IEnumerator FixedCamera_CapturesFullV1AndRapidSwitchLifecycle()
        {
            CharacterDefinition characterDefinition = Resources.Load<CharacterDefinition>("CharacterDefinition");
            CharacterAnimationConfig config = Resources.Load<CharacterAnimationConfig>("CharacterAnimationConfig");
            GameObject character = UnityEngine.Object.Instantiate(characterDefinition.VisualPrefab);
            Animator animator = character.GetComponentInChildren<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var graph = new CharacterAnimationGraph(animator, config);
            var bridge = new CharacterWeaponAnimationBridge(animator, config, graph);
            var runtime = new WeaponRuntime();
            bridge.BindRuntime(runtime);
            graph.Context.IsGrounded = true;

            GameObject cameraObject = new GameObject("RifleLifecycleEvidenceCamera");
            GameObject lightObject = new GameObject("RifleLifecycleEvidenceLight");
            Camera camera = cameraObject.AddComponent<Camera>();
            Light light = lightObject.AddComponent<Light>();
            var target = new RenderTexture(960, 720, 24, RenderTextureFormat.ARGB32);
            string directory = Path.Combine(Application.temporaryCachePath, "RifleLayerLifecycleEvidence");
            Directory.CreateDirectory(directory);
            try
            {
                camera.transform.position = new Vector3(2.6f, 1.45f, 3.2f);
                camera.transform.LookAt(new Vector3(0f, 1.05f, 0f));
                camera.fieldOfView = 34f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
                target.Create();
                camera.targetTexture = target;
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

                yield return Tick(graph, bridge, runtime, 2);
                yield return Capture(camera, Path.Combine(directory, "01-unarmed.png"));
                runtime.RequestEquip(new WeaponId("rifle"));
                yield return Tick(graph, bridge, runtime, 12);
                yield return Capture(camera, Path.Combine(directory, "02-rifle-idle.png"));

                graph.Context.MoveSpeed = 2f;
                graph.Context.WorldVelocity = Vector3.forward * 2f;
                graph.Context.LocalVelocity = Vector3.forward * 2f;
                for (int i = 0; i < 12; i++)
                {
                    bridge.Update(runtime.Snapshot, 25f, -10f, 1f / 60f);
                    graph.Update(1f / 60f);
                    yield return null;
                }
                yield return Capture(camera, Path.Combine(directory, "03-move-aim.png"));

                graph.Context.MoveSpeed = 0f;
                graph.Context.WorldVelocity = Vector3.zero;
                graph.Context.LocalVelocity = Vector3.zero;
                yield return Tick(graph, bridge, runtime, 12);
                yield return Capture(camera, Path.Combine(directory, "04-stop-aim.png"));

                runtime.RequestFire(out _);
                yield return Tick(graph, bridge, runtime, 2);
                yield return Capture(camera, Path.Combine(directory, "05-fire-recoil.png"));

                runtime.RequestUnequip();
                yield return Tick(graph, bridge, runtime, 2);
                yield return Capture(camera, Path.Combine(directory, "06-action-unequip-blend.png"));
                yield return Tick(graph, bridge, runtime, 12);
                yield return Capture(camera, Path.Combine(directory, "07-unarmed-recovered.png"));

                runtime.RequestEquip(new WeaponId("rifle"));
                yield return Tick(graph, bridge, runtime, 1);
                runtime.RequestUnequip();
                yield return Tick(graph, bridge, runtime, 1);
                runtime.RequestEquip(new WeaponId("rifle"));
                yield return Tick(graph, bridge, runtime, 12);
                yield return Capture(camera, Path.Combine(directory, "08-rapid-switch-recovered.png"));
                Assert.AreEqual(WeaponBindingState.Active, bridge.BindingState);
                Assert.IsNotNull(bridge.CurrentPresentation);
                TestContext.Out.WriteLine(directory);
            }
            finally
            {
                bridge.Dispose();
                graph.Dispose();
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(cameraObject);
                UnityEngine.Object.Destroy(lightObject);
                UnityEngine.Object.Destroy(character);
            }
        }

        private static void CreateFixture(
            out GameObject character,
            out CharacterAnimationGraph graph,
            out CharacterWeaponAnimationBridge bridge,
            out WeaponRuntime runtime,
            out ControlledLoader loader,
            out WeaponAnimationDefinition rifleDefinition)
        {
            CharacterDefinition characterDefinition = Resources.Load<CharacterDefinition>("CharacterDefinition");
            CharacterAnimationConfig config = Resources.Load<CharacterAnimationConfig>("CharacterAnimationConfig");
            rifleDefinition = null;
            foreach (WeaponAnimationDefinition definition in config.WeaponDefinitions)
            {
                if (definition != null && definition.WeaponId == new WeaponId("rifle"))
                {
                    rifleDefinition = definition;
                    break;
                }
            }
            Assert.IsNotNull(rifleDefinition);

            character = UnityEngine.Object.Instantiate(characterDefinition.VisualPrefab);
            Animator animator = character.GetComponentInChildren<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            graph = new CharacterAnimationGraph(animator, config);
            loader = new ControlledLoader();
            bridge = new CharacterWeaponAnimationBridge(animator, config, graph, loader);
            runtime = new WeaponRuntime();
            bridge.BindRuntime(runtime);
            graph.Context.IsGrounded = true;
        }

        private static IEnumerator Tick(
            CharacterAnimationGraph graph,
            CharacterWeaponAnimationBridge bridge,
            WeaponRuntime runtime,
            int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                bridge.Update(runtime.Snapshot, 0f, 0f, 1f / 60f);
                graph.Update(1f / 60f);
                yield return null;
            }
        }

        private static bool HasDiagnostic(CharacterAnimationGraph graph, string fragment)
        {
            foreach (AnimationDebugEvent debugEvent in graph.Context.DebugEvents)
            {
                if (debugEvent.EventName.Contains(fragment))
                {
                    return true;
                }
            }
            return false;
        }

        private static IEnumerator Capture(Camera camera, string path)
        {
            RenderTexture target = camera.targetTexture;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.enabled = false;
                yield return new WaitForEndOfFrame();
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
                texture.Apply();
                NormalizeBackground(texture, camera.backgroundColor);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.Destroy(texture);
            }
        }

        private static void NormalizeBackground(Texture2D texture, Color backgroundColor)
        {
            Color32[] pixels = texture.GetPixels32();
            Color32 background = backgroundColor;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].r == 0 && pixels[i].g == 0 && pixels[i].b == 0)
                {
                    pixels[i] = background;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
        }

        private sealed class ControlledLoader : IWeaponPresentationLoader
        {
            public readonly List<Request> Requests = new List<Request>();

            public IDisposable BeginLoad(
                WeaponId weaponId,
                WeaponPresentationLoadTicket ticket,
                Action<WeaponPresentationLoadTicket, WeaponPresentationLoadResult> completed)
            {
                var request = new Request(ticket, completed);
                Requests.Add(request);
                return request;
            }

            public FakeLease CompleteSuccess(int index, WeaponAnimationDefinition definition)
            {
                var lease = new FakeLease(definition, definition.name);
                Request request = Requests[index];
                request.Completed(request.Ticket, new WeaponPresentationLoadResult(lease));
                return lease;
            }

            public FakeLease CompleteFailure(int index, string missingField)
            {
                var lease = new FakeLease(null, "test-definition");
                Request request = Requests[index];
                request.Completed(request.Ticket, new WeaponPresentationLoadResult(lease, missingField));
                return lease;
            }
        }

        private sealed class Request : IDisposable
        {
            public Request(
                WeaponPresentationLoadTicket ticket,
                Action<WeaponPresentationLoadTicket, WeaponPresentationLoadResult> completed)
            {
                Ticket = ticket;
                Completed = completed;
            }

            public WeaponPresentationLoadTicket Ticket { get; }
            public Action<WeaponPresentationLoadTicket, WeaponPresentationLoadResult> Completed { get; }
            public bool IsCancelled { get; private set; }

            public void Dispose()
            {
                IsCancelled = true;
            }
        }

        private sealed class FakeLease : IWeaponPresentationResourceLease
        {
            public FakeLease(WeaponAnimationDefinition definition, string definitionId)
            {
                Definition = definition;
                DefinitionId = definitionId;
            }

            public WeaponAnimationDefinition Definition { get; }
            public string DefinitionId { get; }
            public bool IsReleased { get; private set; }

            public void Dispose()
            {
                IsReleased = true;
            }
        }
    }
}
