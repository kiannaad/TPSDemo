using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
using YooAsset;

namespace CGame.Tests
{
    public class PhysicsInputRuntimeTests : InputTestFixture
    {
        private Keyboard keyboard;
        private object characterTestStep;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();

            Type stepType = Type.GetType("CGame.CharacterTestStep, Assembly-CSharp");
            Assert.IsNotNull(stepType);
            characterTestStep = Activator.CreateInstance(stepType);
            stepType.GetMethod("Enter").Invoke(characterTestStep, null);
        }

        [TearDown]
        public override void TearDown()
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

            keyboard = null;
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator HoldingForwardInput_MovesRuntimeCharacter()
        {
            GameObject character = GameObject.Find("RuntimeCharacter");
            Assert.IsNotNull(character);
            Vector3 startingPosition = character.transform.position;

            Press(keyboard.wKey);
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                yield return new WaitForFixedUpdate();
            }
            Release(keyboard.wKey);
            yield return null;

            Assert.Greater(character.transform.position.z, startingPosition.z + 0.05f);
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
            Press(keyboard.spaceKey);
            yield return null;
            Release(keyboard.spaceKey);

            for (int i = 0; i < 80; i++)
            {
                yield return new WaitForFixedUpdate();
                highestPoint = Mathf.Max(highestPoint, character.transform.position.y);
            }

            Assert.Greater(highestPoint, groundHeight + 0.5f);
            Assert.AreEqual(groundHeight, character.transform.position.y, 0.05f);
            LogAssert.NoUnexpectedReceived();
        }
    }

    public class LaunchCharacterMovementRuntimeTests : InputTestFixture
    {
        private Keyboard keyboard;
        private GameObject launcherObject;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [TearDown]
        public override void TearDown()
        {
            if (launcherObject != null)
            {
                UnityEngine.Object.DestroyImmediate(launcherObject);
                launcherObject = null;
            }

            ReturnLauncherToEmptyState();
            DestroyIfPresent("[CharacterTestRuntime]");
            DestroyIfPresent("[GameManager]");
            keyboard = null;
            base.TearDown();
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
            LogAssert.Expect(LogType.Log, "[CharacterTest] Runtime ready. Use WASD to move and Space to jump.");
            LogAssert.Expect(LogType.Log, new Regex("进入CGame\\.CharacterTestStep时间: \\d+"));

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
            Vector3 startingPosition = character.transform.position;

            Press(keyboard.wKey);
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                yield return new WaitForFixedUpdate();
            }
            Release(keyboard.wKey);
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
            LogAssert.Expect(LogType.Log, "[CharacterTest] Runtime ready. Use WASD to move and Space to jump.");
            LogAssert.Expect(LogType.Log, new Regex(".*CGame\\.CharacterTestStep.*\\d+"));

            launcherObject = new GameObject("LaunchArtCharacterTest");
            launcherObject.AddComponent(launcherMgrType);

            GameObject character = null;
            for (int i = 0; i < 120 && character == null; i++)
            {
                character = GameObject.Find("RuntimeCharacter");
                yield return null;
            }

            Assert.IsNotNull(character);
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
            Press(keyboard.wKey);
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                yield return new WaitForFixedUpdate();
            }
            Release(keyboard.wKey);
            Assert.Greater(character.transform.position.z, startingPosition.z + 0.05f);

            for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
            float groundHeight = character.transform.position.y;
            float highestPoint = groundHeight;
            Press(keyboard.spaceKey);
            yield return null;
            Release(keyboard.spaceKey);
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
}
