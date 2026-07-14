using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using YooAsset;

namespace CGame.Tests
{
    public class PhysicsInputRuntimeTests
    {
        private readonly PlayerInputTestDriver inputDriver = new PlayerInputTestDriver();
        private object characterTestStep;

        [SetUp]
        public void Setup()
        {
            EnsureCharacterStepCreated();
        }

        [UnitySetUp]
        public IEnumerator WaitForCharacterReady()
        {
            EnsureCharacterStepCreated();
            for (int i = 0; i < 120; i++)
            {
                characterTestStep.GetType().GetMethod("Update").Invoke(characterTestStep, null);
                GameObject character = GameObject.Find("RuntimeCharacter");
                if (character != null)
                {
                    inputDriver.Bind(character);
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("CharacterTestStep did not reach CharacterReady within 120 frames.");
        }

        private void EnsureCharacterStepCreated()
        {
            if (characterTestStep != null)
            {
                return;
            }

            Type stepType = Type.GetType("CGame.CharacterTestStep, Assembly-CSharp");
            Assert.IsNotNull(stepType);
            CharacterSpawnTestConfiguration.CreateManagerWithInMemoryDefinition();
            characterTestStep = Activator.CreateInstance(stepType);
            MethodInfo enterMethod = stepType.GetMethod("Enter");
            Assert.IsNotNull(enterMethod);
            enterMethod.Invoke(characterTestStep, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (characterTestStep != null)
            {
                characterTestStep.GetType().GetMethod("Exit").Invoke(characterTestStep, null);
                characterTestStep = null;
            }

            GameObject runtimeRoot = GameObject.Find("[CharacterTestRuntime]");
            if (runtimeRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(runtimeRoot);
            }

            GameObject gameManager = GameObject.Find("[GameManager]");
            if (gameManager != null)
            {
                UnityEngine.Object.DestroyImmediate(gameManager);
            }

            DestroyImmediateIfPresent("[ObserverAimTestRoot]");
            DestroyImmediateIfPresent("Observer Evidence Camera");

            inputDriver.ReleaseAll();
        }

        [UnityTest]
        public IEnumerator CharacterTestStep_Exit_ReleasesRuntimeCharacter()
        {
            characterTestStep.GetType().GetMethod("Exit").Invoke(characterTestStep, null);
            characterTestStep = null;
            yield return null;

            Assert.IsNull(GameObject.Find("RuntimeCharacter"));
        }

        [UnityTest]
        public IEnumerator RuntimeCharacter_IsOwnedOutsideCharacterTestRoot()
        {
            GameObject character = GameObject.Find("RuntimeCharacter");
            GameObject testRoot = GameObject.Find("[CharacterTestRuntime]");
            Assert.NotNull(character);
            Assert.NotNull(testRoot);
            Assert.AreEqual("[CharacterRuntimeRoot]", character.transform.parent.name);
            Assert.IsFalse(character.transform.IsChildOf(testRoot.transform));

            UnityEngine.Object.Destroy(testRoot);
            yield return null;

            Assert.NotNull(GameObject.Find("RuntimeCharacter"));
        }

        [UnityTest]
        public IEnumerator HoldingForwardInput_MovesRuntimeCharacter()
        {
            GameObject character = GameObject.Find("RuntimeCharacter");
            Assert.IsNotNull(character);
            Vector3 startingPosition = character.transform.position;

            ScreenCapture.CaptureScreenshot(GetLocomotionCapturePath("idle.png"));
            yield return new WaitForEndOfFrame();

            inputDriver.SetMove(Vector2.up);
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                yield return new WaitForFixedUpdate();
                if (i == 2)
                {
                    ScreenCapture.CaptureScreenshot(GetLocomotionCapturePath("walk.png"));
                    yield return new WaitForEndOfFrame();
                }
            }
            ScreenCapture.CaptureScreenshot(GetLocomotionCapturePath("sprint.png"));
            yield return new WaitForEndOfFrame();
            inputDriver.ReleaseAll();
            yield return null;

            Assert.Greater(character.transform.position.z, startingPosition.z + 0.05f);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator LookingRightWhileHoldingLeft_MovesAsStrafeAndFacesAimYaw()
        {
            GameObject character = GameObject.Find("RuntimeCharacter");
            Assert.IsNotNull(character);
            Vector3 startingPosition = character.transform.position;
            float groundHeight = startingPosition.y;

            inputDriver.SetLookDelta(new Vector2(90f, 0f));
            yield return null;
            inputDriver.ClearLook();
            inputDriver.SetMove(Vector2.left);
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                yield return new WaitForFixedUpdate();
            }
            inputDriver.ReleaseAll();
            yield return null;

            Assert.Greater(Vector3.Dot(character.transform.forward, Vector3.right), 0.9f);
            Assert.Greater(character.transform.position.z, startingPosition.z + 0.05f);
            Assert.AreEqual(groundHeight, character.transform.position.y, 0.05f);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator LookingRightWhileHoldingBackward_MovesOppositeAimWithoutTurning()
        {
            GameObject character = GameObject.Find("RuntimeCharacter");
            Assert.IsNotNull(character);
            Vector3 startingPosition = character.transform.position;
            float groundHeight = startingPosition.y;

            inputDriver.SetLookDelta(new Vector2(90f, 0f));
            yield return null;
            inputDriver.ClearLook();
            inputDriver.SetMove(Vector2.down);
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                yield return new WaitForFixedUpdate();
            }
            inputDriver.ReleaseAll();
            yield return null;

            Assert.Greater(Vector3.Dot(character.transform.forward, Vector3.right), 0.9f);
            Assert.Less(character.transform.position.x, startingPosition.x - 0.05f);
            Assert.AreEqual(groundHeight, character.transform.position.y, 0.05f);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PressingSpace_JumpsAndReturnsToGround()
        {
            GameObject character = GameObject.Find("RuntimeCharacter");
            Assert.IsNotNull(character);

            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            float groundHeight = character.transform.position.y;
            float highestPoint = groundHeight;
            float previousHeight = groundHeight;
            bool capturedJump = false;
            bool capturedFall = false;
            inputDriver.SetJumpPressed(true);
            yield return null;
            inputDriver.ReleaseAll();

            for (int i = 0; i < 80; i++)
            {
                yield return new WaitForFixedUpdate();
                float currentHeight = character.transform.position.y;
                highestPoint = Mathf.Max(highestPoint, currentHeight);
                if (!capturedJump && currentHeight > groundHeight + 0.2f)
                {
                    ScreenCapture.CaptureScreenshot(GetLocomotionCapturePath("jump.png"));
                    capturedJump = true;
                    yield return new WaitForEndOfFrame();
                }

                if (!capturedFall && capturedJump && currentHeight < previousHeight && currentHeight > groundHeight + 0.2f)
                {
                    ScreenCapture.CaptureScreenshot(GetLocomotionCapturePath("fall.png"));
                    capturedFall = true;
                    yield return new WaitForEndOfFrame();
                }

                previousHeight = currentHeight;
            }

            ScreenCapture.CaptureScreenshot(GetLocomotionCapturePath("land.png"));
            yield return new WaitForEndOfFrame();

            Assert.Greater(highestPoint, groundHeight + 0.5f);
            Assert.IsTrue(capturedJump);
            Assert.IsTrue(capturedFall);
            Assert.AreEqual(groundHeight, character.transform.position.y, 0.05f);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator JumpRenderTrajectory_HasNoSingleFrameVerticalPop()
        {
            GameObject character = GameObject.Find("RuntimeCharacter");
            Assert.IsNotNull(character);
            Animator animator = character.GetComponentInChildren<Animator>();
            Assert.IsNotNull(animator);
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Assert.IsNotNull(hips);

            for (int i = 0; i < 3; i++)
            {
                yield return null;
            }

            float previousCharacterY = character.transform.position.y;
            float previousHipsY = hips.position.y;
            float largestCharacterStep = 0f;
            float largestHipsStep = 0f;
            inputDriver.SetJumpPressed(true);
            yield return null;
            inputDriver.ReleaseAll();

            for (int i = 0; i < 120; i++)
            {
                yield return null;
                float characterStep = character.transform.position.y - previousCharacterY;
                float hipsStep = hips.position.y - previousHipsY;
                largestCharacterStep = Mathf.Max(largestCharacterStep, characterStep);
                largestHipsStep = Mathf.Max(largestHipsStep, hipsStep);
                previousCharacterY = character.transform.position.y;
                previousHipsY = hips.position.y;
            }

            TestContext.WriteLine($"Largest root step: {largestCharacterStep:F4}; largest hips step: {largestHipsStep:F4}");
            Assert.Less(largestCharacterStep, 0.2f);
            Assert.Less(largestHipsStep, 0.2f);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator FirstPersonCamera_UsesUrpViewModelStackWithOneManualBrain()
        {
            yield return new WaitForEndOfFrame();

            Camera[] cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
            Assert.AreEqual(2, cameras.Length);
            Camera worldCamera = Camera.main;
            Assert.NotNull(worldCamera);
            Camera viewModelCamera = null;
            foreach (Camera camera in cameras)
            {
                if (camera != worldCamera)
                {
                    viewModelCamera = camera;
                }
            }

            Assert.NotNull(viewModelCamera);
            Assert.AreEqual("ViewModel Overlay Camera", viewModelCamera.name);

            Type brainType = Type.GetType("Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine");
            Assert.NotNull(brainType);
            Component brain = worldCamera.GetComponent(brainType);
            Assert.NotNull(brain);
            Assert.AreEqual("ManualUpdate", brainType.GetField("UpdateMethod").GetValue(brain).ToString());
            Assert.IsNull(viewModelCamera.GetComponent(brainType));
            Assert.IsNull(worldCamera.GetComponent<UnityEngine.InputSystem.PlayerInput>());
            Assert.IsNull(viewModelCamera.GetComponent<UnityEngine.InputSystem.PlayerInput>());

            Type additionalCameraDataType = Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            Assert.NotNull(additionalCameraDataType);
            Component worldData = worldCamera.GetComponent(additionalCameraDataType);
            Component viewModelData = viewModelCamera.GetComponent(additionalCameraDataType);
            Assert.NotNull(worldData);
            Assert.NotNull(viewModelData);
            Assert.AreEqual("Base", additionalCameraDataType.GetProperty("renderType").GetValue(worldData).ToString());
            Assert.AreEqual("Overlay", additionalCameraDataType.GetProperty("renderType").GetValue(viewModelData).ToString());
            var cameraStack = (IList)additionalCameraDataType.GetProperty("cameraStack").GetValue(worldData);
            CollectionAssert.AreEqual(new[] { viewModelCamera }, cameraStack);

            int ownerWorldBodyLayer = LayerMask.NameToLayer("LocalOwnerWorldBody");
            int viewModelLayer = LayerMask.NameToLayer("FirstPersonViewModel");
            Assert.GreaterOrEqual(ownerWorldBodyLayer, 0);
            Assert.GreaterOrEqual(viewModelLayer, 0);
            Assert.AreEqual(0, worldCamera.cullingMask & (1 << ownerWorldBodyLayer));
            Assert.AreEqual(0, worldCamera.cullingMask & (1 << viewModelLayer));
            Assert.AreEqual(1 << viewModelLayer, viewModelCamera.cullingMask);
            Assert.AreEqual(72f, viewModelCamera.fieldOfView);
            Assert.AreEqual(0.01f, viewModelCamera.nearClipPlane);
            Assert.That(Vector3.Distance(worldCamera.transform.position, viewModelCamera.transform.position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(worldCamera.transform.rotation, viewModelCamera.transform.rotation), Is.LessThan(0.01f));

            Renderer[] allRenderers = UnityEngine.Object.FindObjectsOfType<Renderer>();
            int viewModelRendererCount = 0;
            bool hasTransparentViewModelMaterial = false;
            foreach (Renderer renderer in allRenderers)
            {
                if (renderer.gameObject.layer != viewModelLayer)
                {
                    continue;
                }

                viewModelRendererCount++;
                Assert.IsTrue(renderer.enabled);
                Assert.IsNull(renderer.GetComponent<Collider>());
                Material material = renderer.sharedMaterial;
                hasTransparentViewModelMaterial |= material != null && material.renderQueue >= 3000 && material.color.a < 1f;
            }

            Assert.GreaterOrEqual(viewModelRendererCount, 7);
            Assert.IsTrue(hasTransparentViewModelMaterial, "The ViewModel prototype must exercise transparent rendering.");

            GameObject character = GameObject.Find("RuntimeCharacter");
            Assert.NotNull(character);
            Renderer[] renderers = character.GetComponentsInChildren<Renderer>(true);
            Assert.Greater(renderers.Length, 0);
            foreach (Renderer renderer in renderers)
            {
                Assert.AreEqual(ownerWorldBodyLayer, renderer.gameObject.layer);
                Assert.IsTrue(renderer.enabled);
            }

            foreach (Collider collider in character.GetComponentsInChildren<Collider>(true))
            {
                Assert.AreNotEqual(ownerWorldBodyLayer, collider.gameObject.layer,
                    "Gameplay collider objects must retain their physics layer.");
                Assert.IsTrue(collider.enabled);
            }

            Animator animator = character.GetComponentInChildren<Animator>(true);
            Assert.NotNull(animator);
            Assert.IsTrue(animator.enabled);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ViewModelOverlay_RecordsComparableRenderCost()
        {
            yield return new WaitForEndOfFrame();

            Camera worldCamera = Camera.main;
            Assert.NotNull(worldCamera);
            Camera viewModelCamera = GameObject.Find("ViewModel Overlay Camera")?.GetComponent<Camera>();
            Assert.NotNull(viewModelCamera);

            Type additionalCameraDataType = Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            Assert.NotNull(additionalCameraDataType);
            Component worldData = worldCamera.GetComponent(additionalCameraDataType);
            Assert.NotNull(worldData);
            var cameraStack = (IList)additionalCameraDataType.GetProperty("cameraStack").GetValue(worldData);
            Assert.Contains(viewModelCamera, cameraStack);

            const int warmupFrames = 15;
            const int sampleFrames = 60;
            var drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 128);
            var setPassCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count", 128);
            var cpuFrameTime = ProfilerRecorder.StartNew(ProfilerCategory.Render, "CPU Total Frame Time", 128);

            double baselineDrawCalls = 0d;
            double baselineSetPassCalls = 0d;
            double baselineCpuMilliseconds = 0d;
            double overlayDrawCalls = 0d;
            double overlaySetPassCalls = 0d;
            double overlayCpuMilliseconds = 0d;

            try
            {
                cameraStack.Remove(viewModelCamera);
                viewModelCamera.enabled = false;
                for (int i = 0; i < warmupFrames; i++)
                {
                    yield return new WaitForEndOfFrame();
                }

                for (int i = 0; i < sampleFrames; i++)
                {
                    yield return new WaitForEndOfFrame();
                    baselineDrawCalls += drawCalls.LastValue;
                    baselineSetPassCalls += setPassCalls.LastValue;
                    baselineCpuMilliseconds += cpuFrameTime.LastValue / 1_000_000d;
                }

                viewModelCamera.enabled = true;
                cameraStack.Add(viewModelCamera);
                for (int i = 0; i < warmupFrames; i++)
                {
                    yield return new WaitForEndOfFrame();
                }

                for (int i = 0; i < sampleFrames; i++)
                {
                    yield return new WaitForEndOfFrame();
                    overlayDrawCalls += drawCalls.LastValue;
                    overlaySetPassCalls += setPassCalls.LastValue;
                    overlayCpuMilliseconds += cpuFrameTime.LastValue / 1_000_000d;
                }
            }
            finally
            {
                viewModelCamera.enabled = true;
                if (!cameraStack.Contains(viewModelCamera))
                {
                    cameraStack.Add(viewModelCamera);
                }

                drawCalls.Dispose();
                setPassCalls.Dispose();
                cpuFrameTime.Dispose();
            }

            baselineDrawCalls /= sampleFrames;
            baselineSetPassCalls /= sampleFrames;
            baselineCpuMilliseconds /= sampleFrames;
            overlayDrawCalls /= sampleFrames;
            overlaySetPassCalls /= sampleFrames;
            overlayCpuMilliseconds /= sampleFrames;

            TestContext.WriteLine(
                $"RenderCost samples={sampleFrames}; " +
                $"baseline drawCalls={baselineDrawCalls:F2}, setPass={baselineSetPassCalls:F2}, cpuTotalMs={baselineCpuMilliseconds:F3}; " +
                $"overlay drawCalls={overlayDrawCalls:F2}, setPass={overlaySetPassCalls:F2}, cpuTotalMs={overlayCpuMilliseconds:F3}; " +
                $"delta drawCalls={overlayDrawCalls - baselineDrawCalls:+0.00;-0.00;0.00}, " +
                $"setPass={overlaySetPassCalls - baselineSetPassCalls:+0.00;-0.00;0.00}, " +
                $"cpuTotalMs={overlayCpuMilliseconds - baselineCpuMilliseconds:+0.000;-0.000;0.000}");

            Assert.Greater(baselineDrawCalls, 0d);
            Assert.Greater(overlayDrawCalls, 0d);
            Assert.Greater(baselineCpuMilliseconds, 0d);
            Assert.Greater(overlayCpuMilliseconds, 0d);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator FirstPersonAdsPresentation_UsesOneProgressAndClearsGameplayRejections()
        {
            yield return new WaitForEndOfFrame();

            Type gameManagerType = FindRuntimeType("CGame.GameManager");
            Type cameraManagerType = FindRuntimeType("CGame.CameraManager");
            Assert.NotNull(gameManagerType, "GameManager type missing.");
            Assert.NotNull(cameraManagerType, "CameraManager type missing.");
            MethodInfo getManagerMethod = Array.Find(
                gameManagerType.GetMethods(BindingFlags.Static | BindingFlags.Public),
                method => method.Name == "GetManager" && method.IsGenericMethodDefinition && method.GetParameters().Length == 0);
            Assert.NotNull(getManagerMethod, "GameManager.GetManager<T>() missing.");
            object cameraManager = getManagerMethod.MakeGenericMethod(cameraManagerType).Invoke(null, null);
            Assert.NotNull(cameraManager, "CameraManager instance missing.");
            object adsState = cameraManagerType.GetProperty("AdsPresentationState")?.GetValue(cameraManager);
            object weaponProfile = cameraManagerType.GetProperty("WeaponCameraProfile")?.GetValue(cameraManager);
            Assert.NotNull(adsState, "CameraManager.AdsPresentationState missing.");
            Assert.NotNull(weaponProfile, "CameraManager.WeaponCameraProfile missing.");

            Camera worldCamera = Camera.main;
            Camera viewModelCamera = GameObject.Find("ViewModel Overlay Camera")?.GetComponent<Camera>();
            Transform viewModelRoot = GameObject.Find("First Person ViewModel Prototype")?.transform;
            Assert.NotNull(worldCamera, "World Camera missing.");
            Assert.NotNull(viewModelCamera, "ViewModel Camera missing.");
            Assert.NotNull(viewModelRoot, "ViewModel prototype root missing.");
            Assert.NotNull(inputDriver.Controller, "Bound PlayerController missing.");

            float hipWorldFov = 60f;
            float hipViewModelFov = 72f;
            float adsWorldFov = (float)weaponProfile.GetType().GetProperty("AdsWorldFieldOfView").GetValue(weaponProfile);
            float adsViewModelFov = (float)weaponProfile.GetType().GetProperty("AdsViewModelFieldOfView").GetValue(weaponProfile);
            float adsLookMultiplier = (float)weaponProfile.GetType().GetProperty("AdsLookSensitivityMultiplier").GetValue(weaponProfile);
            Vector3 adsLocalPosition = (Vector3)weaponProfile.GetType().GetProperty("AdsViewModelLocalPosition").GetValue(weaponProfile);

            inputDriver.SetAimHeld(true);
            yield return null;
            yield return new WaitForEndOfFrame();
            float midProgress = (float)adsState.GetType().GetProperty("AdsProgress").GetValue(adsState);
            Assert.That(midProgress, Is.InRange(0.001f, 0.999f));
            AssertAdsConsumersMatch(
                midProgress,
                worldCamera,
                viewModelCamera,
                viewModelRoot,
                inputDriver.Controller,
                hipWorldFov,
                adsWorldFov,
                hipViewModelFov,
                adsViewModelFov,
                adsLookMultiplier,
                adsLocalPosition);

            for (int i = 0; i < 120 && ReadFloat(adsState, "AdsProgress") < 0.999f; i++)
            {
                yield return null;
                yield return new WaitForEndOfFrame();
            }

            AssertAdsConsumersMatch(
                1f,
                worldCamera,
                viewModelCamera,
                viewModelRoot,
                inputDriver.Controller,
                hipWorldFov,
                adsWorldFov,
                hipViewModelFov,
                adsViewModelFov,
                adsLookMultiplier,
                adsLocalPosition);

            string[] rejectionReasons = { "Reloading", "Sprinting", "Dead", "WeaponSwitching" };
            foreach (string rejectionReason in rejectionReasons)
            {
                SetAimRejectionOverride(rejectionReason);
                for (int i = 0; i < 120 && ReadFloat(adsState, "AdsProgress") > 0.001f; i++)
                {
                    yield return null;
                    yield return new WaitForEndOfFrame();
                }

                Assert.AreEqual(rejectionReason, adsState.GetType().GetProperty("RejectionReason").GetValue(adsState).ToString());
                AssertAdsConsumersMatch(
                    0f,
                    worldCamera,
                    viewModelCamera,
                    viewModelRoot,
                    inputDriver.Controller,
                    hipWorldFov,
                    adsWorldFov,
                    hipViewModelFov,
                    adsViewModelFov,
                    adsLookMultiplier,
                    adsLocalPosition);

                characterTestStep.GetType().GetMethod("ClearingAimRejectionOverride").Invoke(characterTestStep, null);
                for (int i = 0; i < 120 && ReadFloat(adsState, "AdsProgress") < 0.999f; i++)
                {
                    yield return null;
                    yield return new WaitForEndOfFrame();
                }
            }

            inputDriver.ReleaseAll();
            for (int i = 0; i < 120 && ReadFloat(adsState, "AdsProgress") > 0.001f; i++)
            {
                yield return null;
                yield return new WaitForEndOfFrame();
            }

            AssertAdsConsumersMatch(
                0f,
                worldCamera,
                viewModelCamera,
                viewModelRoot,
                inputDriver.Controller,
                hipWorldFov,
                adsWorldFov,
                hipViewModelFov,
                adsViewModelFov,
                adsLookMultiplier,
                adsLocalPosition);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator FirstPersonCamera_SamplesPresentedAnchorAndControlRotationInTheSameFrame()
        {
            GameObject character = GameObject.Find("RuntimeCharacter");
            Assert.NotNull(character);
            Type anchorType = Type.GetType("CGame.FirstPersonCameraAnchor, Assembly-CSharp");
            Assert.NotNull(anchorType);
            Component anchor = character.GetComponentInChildren(anchorType, true);
            Assert.NotNull(anchor);
            PropertyInfo anchorPosition = anchorType.GetProperty("Position");
            Type gameManagerType = FindRuntimeType("CGame.GameManager");
            Type cameraManagerType = FindRuntimeType("CGame.CameraManager");
            MethodInfo getManagerMethod = Array.Find(
                gameManagerType.GetMethods(BindingFlags.Static | BindingFlags.Public),
                method => method.Name == "GetManager" && method.IsGenericMethodDefinition && method.GetParameters().Length == 0);
            object cameraManager = getManagerMethod.MakeGenericMethod(cameraManagerType).Invoke(null, null);
            PropertyInfo debugSnapshotProperty = cameraManagerType.GetProperty("DebugSnapshot");
            float maximumBobWeight = 0f;

            inputDriver.SetLookDelta(new Vector2(90f, -20f));
            yield return null;
            inputDriver.ClearLook();
            yield return new WaitForEndOfFrame();
            inputDriver.SetMove(Vector2.up);
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForFixedUpdate();
                yield return new WaitForEndOfFrame();

                Camera camera = Camera.main;
                Assert.NotNull(camera);
                Vector3 presentedPosition = (Vector3)anchorPosition.GetValue(anchor);
                object snapshot = debugSnapshotProperty.GetValue(cameraManager);
                object basePose = snapshot.GetType().GetProperty("BasePose").GetValue(snapshot);
                Vector3 basePosition = (Vector3)basePose.GetType().GetProperty("Position").GetValue(basePose);
                Vector3 finalPosition = (Vector3)snapshot.GetType().GetProperty("Position").GetValue(snapshot);
                Assert.That(Vector3.Distance(basePosition, presentedPosition), Is.LessThan(0.001f),
                    "Locomotion effects must preserve the presented Anchor as Base Pose.");
                Assert.That(Vector3.Distance(camera.transform.position, finalPosition), Is.LessThan(0.001f),
                    "Cinemachine output must consume the composed final Pose in the same frame.");

                IEnumerable contributions = (IEnumerable)snapshot.GetType().GetProperty("Contributions").GetValue(snapshot);
                foreach (object contribution in contributions)
                {
                    if (contribution.GetType().GetProperty("Layer").GetValue(contribution).ToString() != "Bob")
                    {
                        continue;
                    }

                    object poseDelta = contribution.GetType().GetProperty("PoseDelta").GetValue(contribution);
                    maximumBobWeight = Mathf.Max(
                        maximumBobWeight,
                        (float)poseDelta.GetType().GetProperty("Weight").GetValue(poseDelta));
                }
            }

            inputDriver.ReleaseAll();
            for (int i = 0; i < 120; i++)
            {
                yield return new WaitForEndOfFrame();
            }

            object settledSnapshot = debugSnapshotProperty.GetValue(cameraManager);
            float settledBobWeight = 0f;
            foreach (object contribution in (IEnumerable)settledSnapshot.GetType().GetProperty("Contributions").GetValue(settledSnapshot))
            {
                if (contribution.GetType().GetProperty("Layer").GetValue(contribution).ToString() == "Bob")
                {
                    object poseDelta = contribution.GetType().GetProperty("PoseDelta").GetValue(contribution);
                    settledBobWeight = (float)poseDelta.GetType().GetProperty("Weight").GetValue(poseDelta);
                }
            }

            Assert.Greater(maximumBobWeight, 0.05f, "Grounded movement must produce a bounded Bob contribution.");
            Assert.Less(settledBobWeight, 0.01f, "Bob must settle after movement stops.");
            Assert.Greater(Vector3.Dot(Camera.main.transform.forward, Vector3.right), 0.9f);
            Assert.Less(Camera.main.transform.forward.y, -0.2f);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WeaponRecoil_SeparatesAimCameraAndViewModelAndClearsWithoutResidue()
        {
            yield return new WaitForEndOfFrame();

            Type gameManagerType = FindRuntimeType("CGame.GameManager");
            Type cameraManagerType = FindRuntimeType("CGame.CameraManager");
            Type requestType = FindRuntimeType("CGame.WeaponRecoilRequest");
            MethodInfo getManagerMethod = Array.Find(
                gameManagerType.GetMethods(BindingFlags.Static | BindingFlags.Public),
                method => method.Name == "GetManager" && method.IsGenericMethodDefinition && method.GetParameters().Length == 0);
            object cameraManager = getManagerMethod.MakeGenericMethod(cameraManagerType).Invoke(null, null);
            MethodInfo applyRecoil = cameraManagerType.GetMethod("ApplyingWeaponRecoil");
            MethodInfo clearRecoil = cameraManagerType.GetMethod("ClearingWeaponRecoil");
            PropertyInfo debugSnapshot = cameraManagerType.GetProperty("DebugSnapshot");
            Assert.NotNull(cameraManager);
            Assert.NotNull(applyRecoil);
            Assert.NotNull(clearRecoil);
            Assert.NotNull(inputDriver.Controller);

            Quaternion startingAim = (Quaternion)inputDriver.Controller.GetType()
                .GetProperty("ControlRotation").GetValue(inputDriver.Controller);
            object request = Activator.CreateInstance(
                requestType,
                new Vector2(-2f, 0.4f),
                12f,
                new Vector3(0.01f, 0f, -0.025f),
                new Vector3(-2.5f, 0.4f, 0f),
                new Vector3(0f, -0.02f, -0.08f),
                new Vector3(-5f, 0.8f, 0.3f),
                0.7f,
                24f);

            applyRecoil.Invoke(cameraManager, new[] { request });
            Quaternion kickedAim = (Quaternion)inputDriver.Controller.GetType()
                .GetProperty("ControlRotation").GetValue(inputDriver.Controller);
            Assert.Greater(Quaternion.Angle(startingAim, kickedAim), 0.1f,
                "Gameplay recoil must change the authoritative Controller aim immediately.");

            yield return new WaitForEndOfFrame();
            object snapshot = debugSnapshot.GetValue(cameraManager);
            object basePose = snapshot.GetType().GetProperty("BasePose").GetValue(snapshot);
            Quaternion baseRotation = (Quaternion)basePose.GetType().GetProperty("Rotation").GetValue(basePose);
            Quaternion currentAim = (Quaternion)inputDriver.Controller.GetType()
                .GetProperty("ControlRotation").GetValue(inputDriver.Controller);
            Assert.Less(Quaternion.Angle(baseRotation, currentAim), 0.01f,
                "Base camera pose must sample authoritative aim before presentation recoil.");
            Assert.Greater(Quaternion.Angle(baseRotation, (Quaternion)snapshot.GetType().GetProperty("Rotation").GetValue(snapshot)), 0.1f,
                "Visual recoil must alter only the composed camera pose.");

            float visualWeight = FindContributionWeight(snapshot, "VisualRecoil");
            Transform viewModelRoot = GameObject.Find("First Person ViewModel Prototype")?.transform;
            Assert.Greater(visualWeight, 0f);
            Assert.NotNull(viewModelRoot);
            Assert.Greater(Quaternion.Angle(Quaternion.identity, viewModelRoot.localRotation), 0.1f);
            ScreenCapture.CaptureScreenshot(GetRecoilCapturePath("hip-recoil.png"));
            yield return new WaitForEndOfFrame();

            inputDriver.SetAimHeld(true);
            for (int index = 0; index < 30; index++)
            {
                yield return null;
            }

            applyRecoil.Invoke(cameraManager, new[] { request });
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(GetRecoilCapturePath("ads-recoil.png"));
            yield return new WaitForEndOfFrame();

            clearRecoil.Invoke(cameraManager, null);
            yield return new WaitForEndOfFrame();
            object clearedSnapshot = debugSnapshot.GetValue(cameraManager);
            Assert.AreEqual(0f, FindContributionWeight(clearedSnapshot, "VisualRecoil"));
            Assert.Less(Quaternion.Angle(Quaternion.identity, viewModelRoot.localRotation), 0.01f);
            Assert.Less(
                Quaternion.Angle(
                    startingAim,
                    (Quaternion)inputDriver.Controller.GetType().GetProperty("ControlRotation").GetValue(inputDriver.Controller)),
                0.01f,
                "Reload, switch and unbind paths can use the same explicit clear contract without residue.");
            inputDriver.ReleaseAll();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CameraImpulse_IsGeometryConstrainedAndReturnsToStableBasePose()
        {
            for (int index = 0; index < 4; index++)
            {
                yield return new WaitForEndOfFrame();
            }

            Type gameManagerType = FindRuntimeType("CGame.GameManager");
            Type cameraManagerType = FindRuntimeType("CGame.CameraManager");
            Type requestType = FindRuntimeType("CGame.CameraImpulseRequest");
            MethodInfo getManagerMethod = Array.Find(
                gameManagerType.GetMethods(BindingFlags.Static | BindingFlags.Public),
                method => method.Name == "GetManager" && method.IsGenericMethodDefinition && method.GetParameters().Length == 0);
            object cameraManager = getManagerMethod.MakeGenericMethod(cameraManagerType).Invoke(null, null);
            MethodInfo applyImpulse = cameraManagerType.GetMethod("ApplyingCameraImpulse");
            MethodInfo clearImpulse = cameraManagerType.GetMethod("ClearingCameraImpulse");
            PropertyInfo debugSnapshot = cameraManagerType.GetProperty("DebugSnapshot");
            Assert.NotNull(cameraManager);
            Assert.NotNull(applyImpulse);
            Assert.NotNull(clearImpulse);
            Assert.NotNull(inputDriver.Controller);

            object initialSnapshot = debugSnapshot.GetValue(cameraManager);
            object initialBasePose = initialSnapshot.GetType().GetProperty("BasePose").GetValue(initialSnapshot);
            Vector3 basePosition = (Vector3)initialBasePose.GetType().GetProperty("Position").GetValue(initialBasePose);
            Quaternion baseRotation = (Quaternion)initialBasePose.GetType().GetProperty("Rotation").GetValue(initialBasePose);
            Quaternion startingAim = (Quaternion)inputDriver.Controller.GetType()
                .GetProperty("ControlRotation").GetValue(inputDriver.Controller);

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Impulse Validation Wall";
            wall.layer = 2;
            wall.transform.SetParent(GameObject.Find("[GameManager]").transform, true);
            wall.transform.SetPositionAndRotation(
                basePosition + baseRotation * new Vector3(0.032f, 0f, 0.35f),
                baseRotation);
            wall.transform.localScale = new Vector3(0.012f, 3f, 1.2f);
            Collider wallCollider = wall.GetComponent<Collider>();
            Component ownerMotor = GameObject.Find("RuntimeCharacter")
                .GetComponent(FindRuntimeType("CGame.CharacterPhysicsMotor"));
            Assert.NotNull(ownerMotor);
            FieldInfo collidableLayersField = ownerMotor.GetType().GetField("CollidableLayers");
            LayerMask collidableLayers = (LayerMask)collidableLayersField.GetValue(ownerMotor);
            collidableLayersField.SetValue(ownerMotor, (LayerMask)(collidableLayers.value & ~(1 << wall.layer)));

            Physics.SyncTransforms();
            RaycastHit[] validationHits = Physics.SphereCastAll(
                basePosition,
                0.012f,
                baseRotation * Vector3.right,
                0.05f,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);
            Assert.IsTrue(
                Array.Exists(validationHits, hit => hit.collider == wallCollider),
                "Validation wall must be reachable by the same sphere-cast geometry used by the runtime constraint.");
            Type anchorType = FindRuntimeType("CGame.FirstPersonCameraAnchor");
            Component anchor = GameObject.Find("RuntimeCharacter").GetComponentInChildren(anchorType, true);
            Assert.AreNotSame(anchor.transform.root, wall.transform.root,
                "Validation geometry must not be filtered as part of the local owner root.");

            yield return new WaitForEndOfFrame();

            object preImpulseSnapshot = debugSnapshot.GetValue(cameraManager);
            object preImpulseBasePose = preImpulseSnapshot.GetType().GetProperty("BasePose").GetValue(preImpulseSnapshot);
            Vector3 preImpulseBasePosition = (Vector3)preImpulseBasePose.GetType().GetProperty("Position").GetValue(preImpulseBasePose);
            Quaternion preImpulseBaseRotation = (Quaternion)preImpulseBasePose.GetType().GetProperty("Rotation").GetValue(preImpulseBasePose);
            RaycastHit[] preImpulseHits = Physics.SphereCastAll(
                preImpulseBasePosition,
                0.012f,
                preImpulseBaseRotation * Vector3.right,
                0.05f,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);
            Assert.IsTrue(
                Array.Exists(preImpulseHits, hit => hit.collider == wallCollider),
                $"Wall moved out of the current probe: baseDelta={preImpulseBasePosition - basePosition}, " +
                $"rotationDelta={Quaternion.Angle(preImpulseBaseRotation, baseRotation):F3}, wall={wall.transform.position}.");
            Assert.IsFalse(wall.transform.IsChildOf(ownerMotor.transform));
            ScreenCapture.CaptureScreenshot(GetImpulseCapturePath("wall-no-impulse.png"));
            yield return new WaitForEndOfFrame();

            object request = Activator.CreateInstance(
                requestType,
                new Vector3(0.05f, 0f, 0f),
                new Vector3(-2.4f, 0.5f, 0.4f),
                0.4f,
                20f);
            applyImpulse.Invoke(cameraManager, new[] { request });
            yield return new WaitForEndOfFrame();

            object impulseSnapshot = debugSnapshot.GetValue(cameraManager);
            object impulseDelta = FindContributionDelta(impulseSnapshot, "Impulse");
            Vector3 constrainedPosition = (Vector3)impulseDelta.GetType().GetProperty("LocalPosition").GetValue(impulseDelta);
            Assert.Greater(constrainedPosition.magnitude, 0f);
            Assert.Less(constrainedPosition.magnitude, 0.025f,
                "The nearby wall must compress only the requested Impulse translation.");
            Assert.Greater((float)impulseDelta.GetType().GetProperty("Weight").GetValue(impulseDelta), 0f);
            Assert.That(
                (Quaternion)inputDriver.Controller.GetType().GetProperty("ControlRotation").GetValue(inputDriver.Controller),
                Is.EqualTo(startingAim).Using(QuaternionEqualityComparer.Instance),
                "Environmental Camera Impulse must not change authoritative aim.");
            ScreenCapture.CaptureScreenshot(GetImpulseCapturePath("wall-constrained-impulse.png"));
            yield return new WaitForEndOfFrame();

            for (int index = 0; index < 90; index++)
            {
                yield return new WaitForEndOfFrame();
            }

            object settledSnapshot = debugSnapshot.GetValue(cameraManager);
            Assert.AreEqual(0f, FindContributionWeight(settledSnapshot, "Impulse"));
            object settledBasePose = settledSnapshot.GetType().GetProperty("BasePose").GetValue(settledSnapshot);
            Vector3 settledBasePosition = (Vector3)settledBasePose.GetType().GetProperty("Position").GetValue(settledBasePose);
            Vector3 settledFinalPosition = (Vector3)settledSnapshot.GetType().GetProperty("Position").GetValue(settledSnapshot);
            Assert.Less(Vector3.Distance(settledBasePosition, settledFinalPosition), 0.02f,
                "A nearby wall alone must not trigger third-person-style Camera pull-in.");

            applyImpulse.Invoke(cameraManager, new[] { request });
            applyImpulse.Invoke(cameraManager, new[] { request });
            clearImpulse.Invoke(cameraManager, null);
            yield return new WaitForEndOfFrame();
            Assert.AreEqual(0f, FindContributionWeight(debugSnapshot.GetValue(cameraManager), "Impulse"));
            inputDriver.ReleaseAll();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CameraModes_UsePriorityAndCinemachineBlendWithoutChangingGameplayAuthority()
        {
            Type cameraManagerType = FindRuntimeType("CGame.CameraManager");
            Type gameManagerType = FindRuntimeType("CGame.GameManager");
            Type modeType = FindRuntimeType("CGame.CameraMode");
            Type transitionType = FindRuntimeType("CGame.CameraModeTransition");
            Type requestType = FindRuntimeType("CGame.CameraModeRequest");
            Type targetType = FindRuntimeType("CGame.CameraModeTargetState");
            Type poseType = FindRuntimeType("CGame.CameraPose");
            Assert.NotNull(cameraManagerType);
            Assert.NotNull(gameManagerType);
            Assert.NotNull(modeType);
            Assert.NotNull(transitionType);
            Assert.NotNull(requestType);
            Assert.NotNull(targetType);
            Assert.NotNull(poseType);

            MethodInfo getManager = gameManagerType.GetMethod("GetManager", BindingFlags.Public | BindingFlags.Static);
            object cameraManager = getManager.MakeGenericMethod(cameraManagerType).Invoke(null, null);
            MethodInfo requestMode = cameraManagerType.GetMethod("RequestingCameraMode");
            PropertyInfo activeMode = cameraManagerType.GetProperty("ActiveCameraMode");
            object output = cameraManagerType
                .GetField("output", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(cameraManager);
            object brain = output.GetType().GetProperty("Brain").GetValue(output);
            PropertyInfo isBlending = brain.GetType().GetProperty("IsBlending");
            Camera viewModelCamera = (Camera)output.GetType().GetProperty("ViewModelCamera").GetValue(output);
            Assert.NotNull(requestMode);
            Assert.NotNull(activeMode);
            Assert.NotNull(brain);
            Assert.NotNull(isBlending);

            yield return null;
            yield return new WaitForEndOfFrame();
            GameObject character = GameObject.Find("RuntimeCharacter");
            object controller = inputDriver.Controller;
            int characterInstanceId = character.GetInstanceID();
            Camera worldCamera = Camera.main;
            Vector3 gameplayPosition = worldCamera.transform.position;
            Quaternion gameplayRotation = worldCamera.transform.rotation;
            float gameplayFieldOfView = worldCamera.fieldOfView;
            ScreenCapture.CaptureScreenshot(GetCameraModeCapturePath("gameplay-start.png"));
            yield return new WaitForEndOfFrame();

            object respawnTarget = CreateCameraModeTarget(
                targetType,
                poseType,
                gameplayPosition + worldCamera.transform.up * 0.2f,
                gameplayRotation,
                58f);
            IDisposable respawn = RequestCameraMode(
                requestMode, cameraManager, requestType, modeType, transitionType,
                "Respawn", respawnTarget, "Cut", 0f);
            yield return new WaitForEndOfFrame();
            Assert.AreEqual("Respawn", activeMode.GetValue(cameraManager).ToString());
            Assert.IsFalse((bool)isBlending.GetValue(brain));

            Vector3 deathPosition = gameplayPosition + worldCamera.transform.right * 0.8f + Vector3.up * 0.35f;
            object deathTarget = CreateCameraModeTarget(
                targetType,
                poseType,
                deathPosition,
                gameplayRotation,
                50f);
            IDisposable death = RequestCameraMode(
                requestMode, cameraManager, requestType, modeType, transitionType,
                "Death", deathTarget, "EaseInOut", 0.45f);
            yield return new WaitForEndOfFrame();
            Assert.AreEqual("Death", activeMode.GetValue(cameraManager).ToString());
            Assert.IsTrue((bool)isBlending.GetValue(brain), "Death must be selected through a Cinemachine Blend.");
            Assert.IsFalse(viewModelCamera.enabled, "First-person ViewModel must not leak into a non-gameplay mode.");
            ScreenCapture.CaptureScreenshot(GetCameraModeCapturePath("blend-start.png"));
            yield return new WaitForEndOfFrame();

            for (int frame = 0; frame < 3; frame++)
            {
                yield return null;
            }

            Assert.That(worldCamera.fieldOfView, Is.InRange(50f, Mathf.Max(58f, gameplayFieldOfView) + 0.1f));
            Assert.Greater(Vector3.Dot(worldCamera.transform.up, gameplayRotation * Vector3.up), 0.995f,
                "Mode Blend must not introduce a sudden Roll.");
            ScreenCapture.CaptureScreenshot(GetCameraModeCapturePath("blend-mid.png"));
            yield return new WaitForEndOfFrame();

            yield return new WaitForSeconds(0.55f);
            yield return null;

            Assert.IsFalse((bool)isBlending.GetValue(brain));
            Assert.That(Vector3.Distance(worldCamera.transform.position, deathPosition), Is.LessThan(0.03f));
            Assert.AreEqual(50f, worldCamera.fieldOfView, 0.1f);
            ScreenCapture.CaptureScreenshot(GetCameraModeCapturePath("death-end.png"));
            yield return new WaitForEndOfFrame();

            IDisposable spectator = RequestCameraMode(
                requestMode, cameraManager, requestType, modeType, transitionType,
                "Spectator",
                CreateCameraModeTarget(
                    targetType, poseType,
                    gameplayPosition - worldCamera.transform.right * 0.65f + Vector3.up * 0.55f,
                    gameplayRotation, 62f),
                "EaseInOut", 0.25f);
            yield return new WaitForEndOfFrame();
            Assert.AreEqual("Spectator", activeMode.GetValue(cameraManager).ToString());

            IDisposable cinematic = RequestCameraMode(
                requestMode, cameraManager, requestType, modeType, transitionType,
                "Cinematic",
                CreateCameraModeTarget(
                    targetType, poseType,
                    gameplayPosition + Vector3.up * 0.75f,
                    gameplayRotation, 65f),
                "EaseInOut", 0.3f);
            yield return new WaitForEndOfFrame();
            Assert.AreEqual("Cinematic", activeMode.GetValue(cameraManager).ToString());

            cinematic.Dispose();
            yield return new WaitForEndOfFrame();
            Assert.AreEqual("Spectator", activeMode.GetValue(cameraManager).ToString());
            spectator.Dispose();
            yield return new WaitForEndOfFrame();
            Assert.AreEqual("Death", activeMode.GetValue(cameraManager).ToString());
            death.Dispose();
            yield return new WaitForEndOfFrame();
            Assert.AreEqual("Respawn", activeMode.GetValue(cameraManager).ToString());
            respawn.Dispose();
            yield return new WaitForEndOfFrame();
            Assert.AreEqual("GameplayFirstPerson", activeMode.GetValue(cameraManager).ToString());

            for (int frame = 0; frame < 3; frame++)
            {
                yield return null;
            }

            Assert.IsFalse((bool)isBlending.GetValue(brain));
            Assert.IsTrue(viewModelCamera.enabled, "Gameplay ViewModel must be restored after the final Camera Mode releases.");
            Assert.AreEqual(characterInstanceId, GameObject.Find("RuntimeCharacter").GetInstanceID());
            Assert.AreSame(controller, inputDriver.Controller);
            Assert.That(Vector3.Distance(worldCamera.transform.position, gameplayPosition), Is.LessThan(0.15f));
            Assert.AreEqual(gameplayFieldOfView, worldCamera.fieldOfView, 0.2f);
            ScreenCapture.CaptureScreenshot(GetCameraModeCapturePath("gameplay-return.png"));
            yield return new WaitForEndOfFrame();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ObserverAimPresentation_DrivesRemoteWorldBodyWithoutOwnerCameraFacts()
        {
            Type animInstanceType = FindRuntimeType("CGame.Animation.CharacterAnimInstance");
            Type frameDataType = FindRuntimeType("CGame.Animation.CharacterAnimationFrameData");
            Type aimFrameType = FindRuntimeType("CGame.Animation.ObserverAimFrame");
            Type weaponStateType = FindRuntimeType("CGame.Animation.ObserverWeaponState");
            Type presentationType = FindRuntimeType("CGame.ObserverCharacterPresentation");
            Type configType = FindRuntimeType("CGame.Animation.CharacterAnimationConfig");
            Assert.NotNull(animInstanceType, "CharacterAnimInstance type missing.");
            Assert.NotNull(frameDataType, "CharacterAnimationFrameData type missing.");
            Assert.NotNull(aimFrameType, "ObserverAimFrame type missing.");
            Assert.NotNull(weaponStateType, "ObserverWeaponState type missing.");
            Assert.NotNull(presentationType, "ObserverCharacterPresentation type missing.");
            Assert.NotNull(configType, "CharacterAnimationConfig type missing.");

            GameObject owner = GameObject.Find("RuntimeCharacter");
            Assert.NotNull(owner, "Runtime owner character missing.");
            Animator ownerAnimator = owner.GetComponentInChildren<Animator>();
            Assert.NotNull(ownerAnimator, "Owner Animator missing.");
            int ownerInstanceId = owner.GetInstanceID();
            int ownerWorldBodyLayer = LayerMask.NameToLayer("LocalOwnerWorldBody");
            const int observerEvidenceLayer = 31;
            Assert.GreaterOrEqual(ownerWorldBodyLayer, 0);
            Assert.IsTrue(owner.GetComponentsInChildren<Renderer>(true)
                .All(renderer => renderer.gameObject.layer == ownerWorldBodyLayer));

            var observerRoot = new GameObject("[ObserverAimTestRoot]");
            observerRoot.transform.position = owner.transform.position + Vector3.right * 3f;
            GameObject observerVisual = UnityEngine.Object.Instantiate(
                ownerAnimator.transform.gameObject,
                observerRoot.transform);
            observerVisual.name = "ObserverCharacterVisual";
            observerVisual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (Collider collider in observerVisual.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            foreach (Renderer renderer in observerVisual.GetComponentsInChildren<Renderer>(true))
            {
                renderer.gameObject.layer = observerEvidenceLayer;
                renderer.enabled = true;
            }

            Animator observerAnimator = observerVisual.GetComponentInChildren<Animator>();
            Assert.NotNull(observerAnimator, "Cloned observer Animator missing.");
            observerAnimator.applyRootMotion = false;
            UnityEngine.Object config = Resources.Load("CharacterAnimationConfig", configType);
            Assert.NotNull(config, "Runtime CharacterAnimationConfig missing.");
            object animInstance = Activator.CreateInstance(animInstanceType, observerAnimator, config);
            Transform rightHand = observerAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform aimBone = observerAnimator.GetBoneTransform(HumanBodyBones.UpperChest)
                ?? observerAnimator.GetBoneTransform(HumanBodyBones.Chest)
                ?? observerAnimator.GetBoneTransform(HumanBodyBones.Neck)
                ?? observerAnimator.GetBoneTransform(HumanBodyBones.Spine);
            Assert.NotNull(rightHand, "Observer right-hand humanoid bone missing.");
            Assert.NotNull(aimBone, "Observer upper-body humanoid bone missing.");

            GameObject weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weapon.name = "ObserverWeaponPrototype";
            UnityEngine.Object.DestroyImmediate(weapon.GetComponent<Collider>());
            weapon.transform.SetParent(rightHand, false);
            weapon.transform.localPosition = new Vector3(0f, 0.08f, 0.28f);
            weapon.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            weapon.transform.localScale = new Vector3(0.08f, 0.08f, 0.55f);
            weapon.layer = observerEvidenceLayer;

            object presentation = Activator.CreateInstance(
                presentationType,
                observerRoot.transform,
                animInstance,
                weapon);
            object idleFrameData = Activator.CreateInstance(
                frameDataType,
                observerRoot.transform.position,
                Quaternion.identity,
                Vector3.zero,
                Vector3.zero,
                Vector3.zero,
                0f,
                0f,
                true,
                false,
                false,
                0f);

            GameObject observerCameraObject = new GameObject("Observer Evidence Camera");
            Camera observerCamera = observerCameraObject.AddComponent<Camera>();
            Camera[] suppressedCameras = Camera.allCameras.Where(camera => camera != observerCamera).ToArray();
            bool[] suppressedCameraStates = suppressedCameras.Select(camera => camera.enabled).ToArray();
            foreach (Camera camera in suppressedCameras)
            {
                camera.enabled = false;
            }

            observerCamera.depth = 100f;
            observerCamera.clearFlags = CameraClearFlags.SolidColor;
            observerCamera.backgroundColor = new Color(0.36f, 0.52f, 0.72f, 1f);
            observerCamera.cullingMask = 1 << observerEvidenceLayer;
            observerCamera.rect = new Rect(0f, 0f, 1f, 1f);
            Camera.CameraCallback observerCameraViewportLock = camera =>
            {
                if (camera != observerCamera)
                {
                    return;
                }

                camera.rect = new Rect(0f, 0f, 1f, 1f);
                camera.pixelRect = new Rect(0f, 0f, Screen.width, Screen.height);
                camera.aspect = (float)Screen.width / Screen.height;
            };
            Camera.onPreCull += observerCameraViewportLock;
            observerCameraObject.transform.position = observerRoot.transform.position + new Vector3(2.4f, 1.45f, 3.4f);
            observerCameraObject.transform.rotation = Quaternion.LookRotation(
                observerRoot.transform.position + Vector3.up - observerCameraObject.transform.position,
                Vector3.up);

            MethodInfo applyFrame = presentationType.GetMethod("ApplyFrame");
            MethodInfo advance = presentationType.GetMethod("Advance");
            MethodInfo clear = presentationType.GetMethod("Clear");
            MethodInfo updatePhysical = animInstanceType.GetMethod("UpdatePhysicalProperties");
            MethodInfo updateAnimation = animInstanceType.GetMethod("UpdateAnimation");
            object hipFrame = Activator.CreateInstance(
                aimFrameType,
                180f,
                180f,
                0f,
                Enum.Parse(weaponStateType, "HipFire"));
            applyFrame.Invoke(presentation, new[] { hipFrame });
            for (int frame = 0; frame < 20; frame++)
            {
                updatePhysical.Invoke(animInstance, new[] { idleFrameData });
                advance.Invoke(presentation, new object[] { 0.016f });
                updateAnimation.Invoke(animInstance, new object[] { 0.016f });
                yield return null;
            }

            Quaternion neutralAimBoneRotation = aimBone.rotation;
            Assert.IsTrue(weapon.activeSelf);
            PrepareObserverEvidenceCamera(observerCamera);
            yield return new WaitForEndOfFrame();
            CaptureScreen(GetObserverAimCapturePath("observer-hipfire-neutral.png"));

            object adsFrame = Activator.CreateInstance(
                aimFrameType,
                180f,
                225f,
                35f,
                Enum.Parse(weaponStateType, "Ads"));
            applyFrame.Invoke(presentation, new[] { adsFrame });
            for (int frame = 0; frame < 24; frame++)
            {
                updatePhysical.Invoke(animInstance, new[] { idleFrameData });
                advance.Invoke(presentation, new object[] { 0.016f });
                updateAnimation.Invoke(animInstance, new object[] { 0.016f });
                yield return null;
            }

            object snapshot = presentationType.GetProperty("Snapshot").GetValue(presentation);
            Assert.AreEqual("Ads", snapshot.GetType().GetProperty("WeaponState").GetValue(snapshot).ToString());
            Assert.AreEqual(1f, (float)snapshot.GetType().GetProperty("AimWeight").GetValue(snapshot), 0.001f);
            Assert.AreEqual(1f, (float)snapshot.GetType().GetProperty("AdsWeight").GetValue(snapshot), 0.001f);
            Assert.AreEqual(1f, (float)snapshot.GetType().GetProperty("LeftHandIkWeight").GetValue(snapshot), 0.001f);
            Assert.AreEqual(180f, observerRoot.transform.eulerAngles.y, 0.1f);
            float observerAimBoneAngle = Quaternion.Angle(neutralAimBoneRotation, aimBone.rotation);
            Assert.Greater(observerAimBoneAngle, 3f);
            Assert.Less(observerAimBoneAngle, 70f, "Observer upper-body aim must remain inside a stable presentation range.");
            Assert.IsTrue(observerAnimator.enabled);
            Assert.IsTrue(weapon.activeSelf);
            Assert.IsTrue(observerVisual.GetComponentsInChildren<Renderer>(true).All(renderer => renderer.enabled));
            Assert.IsTrue(observerVisual.GetComponentsInChildren<Renderer>(true)
                .All(renderer => renderer.gameObject.layer != ownerWorldBodyLayer));
            Assert.AreEqual(ownerInstanceId, GameObject.Find("RuntimeCharacter").GetInstanceID());
            PrepareObserverEvidenceCamera(observerCamera);
            yield return new WaitForEndOfFrame();
            CaptureScreen(GetObserverAimCapturePath("observer-ads-aim-up-right.png"));

            clear.Invoke(presentation, null);
            for (int frame = 0; frame < 12; frame++)
            {
                updatePhysical.Invoke(animInstance, new[] { idleFrameData });
                advance.Invoke(presentation, new object[] { 0.016f });
                updateAnimation.Invoke(animInstance, new object[] { 0.016f });
                yield return null;
            }

            object cleared = presentationType.GetProperty("Snapshot").GetValue(presentation);
            Assert.IsFalse((bool)cleared.GetType().GetProperty("IsActive").GetValue(cleared));
            Assert.AreEqual(0f, (float)cleared.GetType().GetProperty("AimWeight").GetValue(cleared), 0.001f);
            Assert.AreEqual(0f, (float)cleared.GetType().GetProperty("LeftHandIkWeight").GetValue(cleared), 0.001f);
            Assert.IsFalse(weapon.activeSelf);
            PrepareObserverEvidenceCamera(observerCamera);
            yield return new WaitForEndOfFrame();
            CaptureScreen(GetObserverAimCapturePath("observer-cleared.png"));

            ((IDisposable)animInstance).Dispose();
            Camera.onPreCull -= observerCameraViewportLock;
            for (int cameraIndex = 0; cameraIndex < suppressedCameras.Length; cameraIndex++)
            {
                if (suppressedCameras[cameraIndex] != null)
                {
                    suppressedCameras[cameraIndex].enabled = suppressedCameraStates[cameraIndex];
                }
            }

            UnityEngine.Object.DestroyImmediate(observerCameraObject);
            UnityEngine.Object.DestroyImmediate(observerRoot);
            LogAssert.NoUnexpectedReceived();
        }

        private void SetAimRejectionOverride(string rejectionReason)
        {
            Type rejectionType = FindRuntimeType("CGame.AimRejectionReason");
            Assert.NotNull(rejectionType);
            object reason = Enum.Parse(rejectionType, rejectionReason);
            MethodInfo method = characterTestStep.GetType().GetMethod("SettingAimRejectionOverride");
            Assert.NotNull(method);
            method.Invoke(characterTestStep, new[] { reason });
        }

        private static float ReadFloat(object target, string propertyName)
        {
            return (float)target.GetType().GetProperty(propertyName).GetValue(target);
        }

        private static Type FindRuntimeType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static string GetLocomotionCapturePath(string fileName)
        {
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "HarnessCaptures010"));
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, fileName);
        }

        private static string GetRecoilCapturePath(string fileName)
        {
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "HarnessCaptures011"));
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, fileName);
        }

        private static string GetImpulseCapturePath(string fileName)
        {
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "HarnessCaptures012"));
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, fileName);
        }

        private static string GetCameraModeCapturePath(string fileName)
        {
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "HarnessCaptures013"));
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, fileName);
        }

        private static string GetObserverAimCapturePath(string fileName)
        {
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "HarnessCaptures014"));
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, fileName);
        }

        private static void DestroyImmediateIfPresent(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void CaptureScreen(string path)
        {
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void PrepareObserverEvidenceCamera(Camera camera)
        {
            camera.rect = new Rect(0f, 0f, 1f, 1f);
            camera.pixelRect = new Rect(0f, 0f, Screen.width, Screen.height);
            camera.aspect = (float)Screen.width / Screen.height;
            camera.ResetProjectionMatrix();
            Assert.AreEqual(Screen.width, camera.pixelWidth);
            Assert.AreEqual(Screen.height, camera.pixelHeight);
        }

        private static object CreateCameraModeTarget(
            Type targetType,
            Type poseType,
            Vector3 position,
            Quaternion rotation,
            float fieldOfView)
        {
            object target = Activator.CreateInstance(targetType);
            object pose = Activator.CreateInstance(poseType, position, rotation);
            targetType.GetMethod("Updating").Invoke(target, new[] { pose, (object)fieldOfView });
            return target;
        }

        private static IDisposable RequestCameraMode(
            MethodInfo requestMode,
            object cameraManager,
            Type requestType,
            Type modeType,
            Type transitionType,
            string mode,
            object target,
            string transition,
            float duration)
        {
            object request = Activator.CreateInstance(
                requestType,
                Enum.Parse(modeType, mode),
                target,
                Enum.Parse(transitionType, transition),
                duration);
            return (IDisposable)requestMode.Invoke(cameraManager, new[] { request });
        }

        private static object FindContributionDelta(object snapshot, string layerName)
        {
            foreach (object contribution in (IEnumerable)snapshot.GetType().GetProperty("Contributions").GetValue(snapshot))
            {
                if (contribution.GetType().GetProperty("Layer").GetValue(contribution).ToString() == layerName)
                {
                    return contribution.GetType().GetProperty("PoseDelta").GetValue(contribution);
                }
            }

            Assert.Fail($"Camera contribution was not found: {layerName}");
            return null;
        }

        private static float FindContributionWeight(object snapshot, string layerName)
        {
            foreach (object contribution in (IEnumerable)snapshot.GetType().GetProperty("Contributions").GetValue(snapshot))
            {
                if (contribution.GetType().GetProperty("Layer").GetValue(contribution).ToString() != layerName)
                {
                    continue;
                }

                object poseDelta = contribution.GetType().GetProperty("PoseDelta").GetValue(contribution);
                return (float)poseDelta.GetType().GetProperty("Weight").GetValue(poseDelta);
            }

            Assert.Fail($"Camera contribution was not found: {layerName}");
            return 0f;
        }

        private static void AssertAdsConsumersMatch(
            float expectedProgress,
            Camera worldCamera,
            Camera viewModelCamera,
            Transform viewModelRoot,
            object controller,
            float hipWorldFov,
            float adsWorldFov,
            float hipViewModelFov,
            float adsViewModelFov,
            float adsLookMultiplier,
            Vector3 adsLocalPosition)
        {
            Assert.AreEqual(Mathf.Lerp(hipWorldFov, adsWorldFov, expectedProgress), worldCamera.fieldOfView, 0.05f);
            Assert.AreEqual(Mathf.Lerp(hipViewModelFov, adsViewModelFov, expectedProgress), viewModelCamera.fieldOfView, 0.05f);
            Assert.AreEqual(
                Mathf.Lerp(1f, adsLookMultiplier, expectedProgress),
                ReadFloat(controller, "LookSensitivityMultiplier"),
                0.001f);
            Assert.That(Vector3.Distance(Vector3.Lerp(Vector3.zero, adsLocalPosition, expectedProgress), viewModelRoot.localPosition),
                Is.LessThan(0.001f));
        }

    }

    public sealed class CharacterTestStepLifecycleTests
    {
        private object characterTestStep;

        [TearDown]
        public void TearDown()
        {
            characterTestStep?.GetType().GetMethod("Exit").Invoke(characterTestStep, null);
            characterTestStep = null;
            DestroyIfPresent("[CharacterTestRuntime]");
            DestroyIfPresent("[GameManager]");
        }

        [UnityTest]
        public IEnumerator ExitBeforeObservingReady_ReleasesRuntimeCharacter()
        {
            Type stepType = Type.GetType("CGame.CharacterTestStep, Assembly-CSharp");
            Assert.NotNull(stepType);
            CharacterSpawnTestConfiguration.CreateManagerWithInMemoryDefinition();
            characterTestStep = Activator.CreateInstance(stepType);
            stepType.GetMethod("Enter").Invoke(characterTestStep, null);
            for (int i = 0; i < 120 && GameObject.Find("RuntimeCharacter") == null; i++)
            {
                yield return null;
            }

            Assert.NotNull(GameObject.Find("RuntimeCharacter"));

            characterTestStep.GetType().GetMethod("Exit").Invoke(characterTestStep, null);
            characterTestStep = null;
            Assert.NotNull(GameObject.Find("RuntimeCharacter"));

            yield return null;

            Assert.IsNull(GameObject.Find("RuntimeCharacter"));
        }

        private static void DestroyIfPresent(string objectName)
        {
            GameObject gameObject = GameObject.Find(objectName);
            if (gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }

    public class LaunchCharacterMovementRuntimeTests
    {
        private readonly PlayerInputTestDriver inputDriver = new PlayerInputTestDriver();
        private GameObject launcherObject;

        [SetUp]
        public void Setup()
        {
            FieldInfo settingField = typeof(YooAssetSettingsData).GetField("_setting", BindingFlags.Static | BindingFlags.NonPublic);
            if (settingField?.GetValue(null) == null)
            {
                LogAssert.Expect(LogType.Log, new Regex("YooAsset use (default|user) settings\\."));
                YooAssetSettingsData.GetDefaultYooFolderName();
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (launcherObject != null)
            {
                UnityEngine.Object.DestroyImmediate(launcherObject);
                launcherObject = null;
            }

            ReturnLauncherToEmptyState();
            DestroyIfPresent("[CharacterTestRuntime]");
            DestroyIfPresent("[GameManager]");
            inputDriver.ReleaseAll();
        }

        [UnityTearDown]
        public IEnumerator TearDownResourcePackage()
        {
            ResourcePackage package = YooAssets.TryGetPackage("DefaultPackage");
            if (package == null)
            {
                yield break;
            }

            yield return package.DestroyAsync();
            YooAssets.RemovePackage("DefaultPackage");
            YooAssets.Destroy();
        }

        [UnityTest]
        public IEnumerator SampleSceneLaunch_HoldingForwardInput_MovesRuntimeCharacter()
        {
            ReturnLauncherToEmptyState();
            Type launcherMgrType = Type.GetType("CGame.GameLauncherMgr, Assembly-CSharp");
            Assert.IsNotNull(launcherMgrType);

            LogAssert.Expect(LogType.Log, new Regex("进入CGame\\.PreSourceStep时间: \\d+"));
            LogAssert.Expect(LogType.Log, "YooAssets initialize !");
            LogAssert.Expect(LogType.Log, "Create resource package : DefaultPackage");
            LogAssert.Expect(LogType.Log, "The package DefaultPackage create file system : YooAsset.DefaultEditorFileSystem");
            LogAssert.Expect(LogType.Log, "<color=green>ResourceManager: DefaultPackage initialized successfully!</color>");
            LogAssert.Expect(LogType.Log, new Regex("退出CGame\\.PreSourceStep时间: \\d+"));
            LogAssert.Expect(LogType.Log, new Regex("进入CGame\\.EnterStep时间: \\d+"));
            LogAssert.Expect(LogType.Log, new Regex("退出CGame\\.EnterStep时间: \\d+"));
            LogAssert.Expect(LogType.Log, new Regex("进入CGame\\.CharacterTestStep时间: \\d+"));
            LogAssert.Expect(LogType.Log, "[CharacterTest] Runtime ready. Use WASD to move and Space to jump.");

            launcherObject = new GameObject("LaunchRuntimeTest");
            launcherObject.AddComponent(launcherMgrType);

            GameObject character = null;
            for (int i = 0; i < 120; i++)
            {
                character = GameObject.Find("RuntimeCharacter");
                if (character != null)
                {
                    break;
                }

                yield return null;
            }

            Assert.IsNotNull(character);
            inputDriver.Bind(character);
            Vector3 startingPosition = character.transform.position;

            inputDriver.SetMove(Vector2.up);
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                yield return new WaitForFixedUpdate();
            }
            inputDriver.ReleaseAll();
            yield return null;

            Assert.Greater(character.transform.position.z, startingPosition.z + 0.05f);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SampleSceneLaunch_ArtCharacterMovesJumpsAndLands()
        {
            ReturnLauncherToEmptyState();
            Type launcherMgrType = Type.GetType("CGame.GameLauncherMgr, Assembly-CSharp");
            Assert.IsNotNull(launcherMgrType);

            LogAssert.Expect(LogType.Log, new Regex(".*CGame\\.PreSourceStep.*\\d+"));
            LogAssert.Expect(LogType.Log, "YooAssets initialize !");
            LogAssert.Expect(LogType.Log, "Create resource package : DefaultPackage");
            LogAssert.Expect(LogType.Log, "The package DefaultPackage create file system : YooAsset.DefaultEditorFileSystem");
            LogAssert.Expect(LogType.Log, "<color=green>ResourceManager: DefaultPackage initialized successfully!</color>");
            LogAssert.Expect(LogType.Log, new Regex(".*CGame\\.PreSourceStep.*\\d+"));
            LogAssert.Expect(LogType.Log, new Regex(".*CGame\\.EnterStep.*\\d+"));
            LogAssert.Expect(LogType.Log, new Regex(".*CGame\\.EnterStep.*\\d+"));
            LogAssert.Expect(LogType.Log, new Regex(".*CGame\\.CharacterTestStep.*\\d+"));
            LogAssert.Expect(LogType.Log, "[CharacterTest] Runtime ready. Use WASD to move and Space to jump.");

            launcherObject = new GameObject("LaunchArtCharacterTest");
            launcherObject.AddComponent(launcherMgrType);

            GameObject character = null;
            for (int i = 0; i < 120 && character == null; i++)
            {
                character = GameObject.Find("RuntimeCharacter");
                yield return null;
            }

            Assert.IsNotNull(character);
            inputDriver.Bind(character);
            Transform visual = character.transform.Find("CharacterVisual");
            Assert.IsNotNull(visual);
            SkinnedMeshRenderer[] renderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>();
            Assert.Greater(renderers.Length, 0);
            Assert.IsTrue(System.Array.Exists(renderers, renderer => renderer.enabled && renderer.gameObject.activeInHierarchy));
            Assert.IsNull(character.transform.Find("Visual"));
            Animator animator = visual.GetComponentInChildren<Animator>();
            Assert.IsNotNull(animator);
            Assert.IsTrue(animator.hasBoundPlayables);

            Vector3 startingPosition = character.transform.position;
            inputDriver.SetMove(Vector2.up);
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                yield return new WaitForFixedUpdate();
            }
            inputDriver.ReleaseAll();
            Assert.Greater(character.transform.position.z, startingPosition.z + 0.05f);

            for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
            float groundHeight = character.transform.position.y;
            float highestPoint = groundHeight;
            inputDriver.SetJumpPressed(true);
            yield return null;
            inputDriver.ReleaseAll();
            for (int i = 0; i < 100; i++)
            {
                yield return new WaitForFixedUpdate();
                highestPoint = Mathf.Max(highestPoint, character.transform.position.y);
            }

            Assert.Greater(highestPoint, groundHeight + 0.5f);
            Assert.AreEqual(groundHeight, character.transform.position.y, 0.05f);
            Assert.IsTrue(visual.gameObject.activeInHierarchy);
            LogAssert.NoUnexpectedReceived();
        }

        private static void DestroyIfPresent(string objectName)
        {
            GameObject gameObject = GameObject.Find(objectName);
            if (gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static void ReturnLauncherToEmptyState()
        {
            Type launcherType = Type.GetType("CGame.GameLauncher, Assembly-CSharp");
            if (launcherType == null)
            {
                return;
            }

            object launcher = GetStaticInstance(launcherType);
            if (launcher == null)
            {
                return;
            }

            launcherType.GetMethod("ReturnLoginPanel")?.Invoke(launcher, null);
        }

        private static object GetStaticInstance(Type type)
        {
            while (type != null)
            {
                var property = type.GetProperty("Instance");
                if (property != null)
                {
                    return property.GetValue(null);
                }

                type = type.BaseType;
            }

            return null;
        }
    }

    internal sealed class PlayerInputTestDriver
    {
        private PlayerInputState state;

        public object Controller { get; private set; }

        public void Bind(GameObject character)
        {
            Type pawnHostType = Type.GetType("CGame.PawnHost, Assembly-CSharp");
            Assert.IsNotNull(pawnHostType);
            Component pawnHost = character.GetComponent(pawnHostType);
            Assert.IsNotNull(pawnHost);

            object pawn = pawnHostType.GetProperty("Pawn")?.GetValue(pawnHost);
            Assert.IsNotNull(pawn);
            object controller = pawn.GetType().GetProperty("Controller")?.GetValue(pawn);
            Assert.IsNotNull(controller);
            Controller = controller;

            MethodInfo setProviderMethod = controller.GetType().GetMethod("SettingInputStateProvider");
            Assert.IsNotNull(setProviderMethod);
            setProviderMethod.Invoke(controller, new object[] { new Func<PlayerInputState>(() => state) });
        }

        public void SetMove(Vector2 moveInput)
        {
            state.MoveInput = moveInput;
        }

        public void SetLookDelta(Vector2 lookDelta)
        {
            state.LookInput = new LookInputValue(lookDelta, LookInputTimeMode.Delta);
        }

        public void ClearLook()
        {
            state.LookInput = default;
        }

        public void SetJumpPressed(bool jumpPressed)
        {
            state.JumpPressed = jumpPressed;
        }

        public void SetAimHeld(bool aimHeld)
        {
            state.AimHeld = aimHeld;
        }

        public void ReleaseAll()
        {
            state = default;
        }
    }
}
