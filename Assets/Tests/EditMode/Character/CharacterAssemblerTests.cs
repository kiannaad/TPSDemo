using System;
using System.Linq;
using System.Reflection;
using CGame.Animation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CGame.Tests
{
    public sealed class CharacterAssemblerTests
    {
        [Test]
        public void Assemble_CreatesInactiveUnregisteredCharacterAssembly()
        {
            CharacterDefinition definition = CreateValidDefinition();
            GameObject parent = new GameObject("CharacterAssemblerParent");
            try
            {
                object assembly = Assemble(definition, parent.transform, "AssembledCharacter");
                GameObject root = GetProperty<GameObject>(assembly, "Root");

                Assert.IsFalse(root.activeSelf);
                Assert.AreSame(parent.transform, root.transform.parent);
                Assert.IsNotNull(root.GetComponent("PawnHost"));
                Assert.IsNotNull(root.GetComponent("CharacterPhysicsMotor"));
                Assert.IsNotNull(GetProperty<object>(assembly, "Character"));
                Assert.IsNotNull(GetProperty<Component>(assembly, "Animator"));
                Assert.IsNotNull(GetProperty<object>(assembly, "Movement"));
                object animationComponent = GetProperty<object>(assembly, "AnimationComponent");
                Assert.IsNotNull(animationComponent);
                Assert.AreSame(definition.AnimationConfig, animationComponent.GetType().GetField("config", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(animationComponent));
                Assert.IsEmpty(GetAssemblerType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic));

                ((IDisposable)assembly).Dispose();
                Assert.IsTrue(root == null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void Assemble_RejectsVisualWithoutAnimatorWithoutLeavingRoot()
        {
            CharacterDefinition definition = CreateValidDefinition();
            GameObject invalidVisual = new GameObject("CharacterAssemblerInvalidVisual");
            SetField(definition, "visualPrefab", invalidVisual);
            try
            {
                TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => Assemble(definition, null, "FailedCharacterAssembly"));

                Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);
                Assert.IsNull(GameObject.Find("FailedCharacterAssembly"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(invalidVisual);
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void Assemble_RejectsInvalidDefinitionBeforeCreatingRoot()
        {
            CharacterDefinition definition = CreateValidDefinition();
            SetField(definition, "animationConfig", null);
            try
            {
                TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => Assemble(definition, null, "InvalidDefinitionAssembly"));

                Assert.IsInstanceOf<ArgumentException>(exception.InnerException);
                Assert.IsNull(GameObject.Find("InvalidDefinitionAssembly"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        private static object Assemble(CharacterDefinition definition, Transform parent, string name)
        {
            MethodInfo assembleMethod = GetAssemblerType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Single(method => method.Name == "Assemble" && method.GetParameters().Length == 5);
            return assembleMethod.Invoke(Activator.CreateInstance(GetAssemblerType()), new object[]
            {
                definition,
                parent,
                Vector3.zero,
                Quaternion.identity,
                name,
            });
        }

        private static Type GetAssemblerType()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("CGame.CharacterAssembler"))
                .FirstOrDefault(type => type != null);
        }

        private static T GetProperty<T>(object instance, string name)
        {
            return (T)instance.GetType().GetProperty(name).GetValue(instance);
        }

        private static CharacterDefinition CreateValidDefinition()
        {
            CharacterDefinition definition = ScriptableObject.CreateInstance<CharacterDefinition>();
            SetField(definition, "definitionId", "assembly-test");
            SetField(definition, "visualPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Animation/FemaleLocomotionSet/Prefabs/Robot Kyle.prefab"));
            SetField(definition, "animationConfig", Resources.Load<CharacterAnimationConfig>("CharacterAnimationConfig"));
            SetField(definition, "supportedControlKinds", new[] { CharacterControlKind.LocalPlayer });
            return definition;
        }

        private static void SetField(CharacterDefinition definition, string name, object value)
        {
            typeof(CharacterDefinition).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(definition, value);
        }
    }
}
