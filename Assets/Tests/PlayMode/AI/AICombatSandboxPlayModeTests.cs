using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace CGame.Tests
{
    public sealed class AICombatSandboxPlayModeTests
    {
        private const string SandboxScenePath = "Assets/Scenes/AICombatSandbox.unity";
        private const string CleanupScenePath = "Assets/Scenes/SampleScene.unity";

        [UnityTest]
        [Explicit("Loads the complete AICombatSandbox scene and owns the shared runtime for its duration.")]
        public IEnumerator DirectScenePlay_SpawnsPlayerAndSixFormalAI()
        {
#if UNITY_EDITOR
            AsyncOperation load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                SandboxScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Direct scene loading is an Editor-only acceptance check.");
            yield break;
#endif
            Assert.NotNull(load);
            while (!load.isDone)
            {
                yield return null;
            }

            Component bootstrap = null;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < deadline)
            {
                GameObject sandboxRoot = GameObject.Find("AICombatSandbox");
                bootstrap = sandboxRoot != null
                    ? sandboxRoot.GetComponent("AICombatSandboxBootstrap")
                    : null;
                if (bootstrap != null && GetProperty<bool>(bootstrap, "IsReady"))
                {
                    break;
                }

                yield return null;
            }

            Assert.NotNull(bootstrap, "The sandbox scene must contain its runtime bootstrap.");
            Assert.IsTrue(GetProperty<bool>(bootstrap, "PlayerReady"), GetProperty<string>(bootstrap, "FailureMessage"));
            Assert.AreEqual(6, GetProperty<int>(bootstrap, "ReadyAICount"));
            Assert.AreEqual(6, GetProperty<int>(bootstrap, "RequestedAICount"));
            Assert.NotNull(GameObject.Find("SandboxPlayer"));
            for (int i = 1; i <= 6; i++)
            {
                Assert.NotNull(GameObject.Find($"SandboxAI-{i}"), $"SandboxAI-{i} was not spawned.");
            }

            Assert.IsNotNull(GetProperty<object>(bootstrap, "PlayerHealth"));
            LogAssert.NoUnexpectedReceived();

#if UNITY_EDITOR
            GameObject gameManager = GameObject.Find("[GameManager]");
            if (gameManager != null)
            {
                Object.Destroy(gameManager);
                yield return null;
                yield return null;
            }

            AsyncOperation cleanup = EditorSceneManager.LoadSceneAsyncInPlayMode(
                CleanupScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            Assert.NotNull(cleanup);
            while (!cleanup.isDone)
            {
                yield return null;
            }
#endif
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(property, $"Missing property {propertyName} on {target.GetType().FullName}.");
            return (T)property.GetValue(target);
        }
    }
}
