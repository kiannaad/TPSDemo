using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
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
        public IEnumerator HoldingLeftInput_RotatesRuntimeCharacterTowardMovement()
        {
            GameObject character = GameObject.Find("RuntimeCharacter");
            Assert.IsNotNull(character);

            inputDriver.SetMove(Vector2.left);
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                yield return new WaitForFixedUpdate();
            }
            inputDriver.ReleaseAll();
            yield return null;

            Assert.Greater(Vector3.Dot(character.transform.forward, Vector3.left), 0.9f);
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
            inputDriver.SetJumpPressed(true);
            yield return null;
            inputDriver.ReleaseAll();

            for (int i = 0; i < 80; i++)
            {
                yield return new WaitForFixedUpdate();
                highestPoint = Mathf.Max(highestPoint, character.transform.position.y);
            }

            Assert.Greater(highestPoint, groundHeight + 0.5f);
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

            MethodInfo setProviderMethod = controller.GetType().GetMethod("SettingInputStateProvider");
            Assert.IsNotNull(setProviderMethod);
            setProviderMethod.Invoke(controller, new object[] { new Func<PlayerInputState>(() => state) });
        }

        public void SetMove(Vector2 moveInput)
        {
            state.MoveInput = moveInput;
        }

        public void SetJumpPressed(bool jumpPressed)
        {
            state.JumpPressed = jumpPressed;
        }

        public void ReleaseAll()
        {
            state = default;
        }
    }
}
