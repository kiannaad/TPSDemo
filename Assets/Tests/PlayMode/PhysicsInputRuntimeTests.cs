using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

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
}
