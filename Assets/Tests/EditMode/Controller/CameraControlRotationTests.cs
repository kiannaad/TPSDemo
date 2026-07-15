using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CGame.Tests
{
    public class CameraControlRotationTests
    {
        /// <summary>
        /// 验证 Controller 连续推进 Yaw、限制 Pitch，并从权威旋转中清除 Roll。
        /// </summary>
        [Test]
        public void ControllerUpdate_ProducesConstrainedYawPitchWithoutRoll()
        {
            Type controllerType = RequireRuntimeType("CGame.Controller");
            object controller = Activator.CreateInstance(controllerType);

            Invoke(controller, "SettingPitchLimits", -70f, 80f);
            Invoke(controller, "SettingControlRotation", Quaternion.Euler(10f, 350f, 25f));
            Invoke(controller, "AddingYawInput", 20f);
            Invoke(controller, "AddingPitchInput", 100f);
            Invoke(controller, "UpdatingController", 0.016f);

            Assert.AreEqual(370f, ReadProperty<float>(controller, "ControlYaw"), 0.0001f);
            Assert.AreEqual(80f, ReadProperty<float>(controller, "ControlPitch"), 0.0001f);

            Quaternion controlRotation = ReadProperty<Quaternion>(controller, "ControlRotation");
            Quaternion expectedRotation = Quaternion.Euler(80f, 370f, 0f);
            Assert.Less(Quaternion.Angle(expectedRotation, controlRotation), 0.001f);
        }

        /// <summary>
        /// 验证 PlayerController 当帧消费 Pointer Delta，并把向上的输入转换为正向抬头语义。
        /// </summary>
        [Test]
        public void PlayerControllerUpdate_AppliesPointerLookWithoutDeltaTimeScaling()
        {
            Type controllerType = RequireRuntimeType("CGame.PlayerController");
            object controller = Activator.CreateInstance(controllerType);
            PlayerInputState state = new PlayerInputState
            {
                LookInput = new LookInputValue(new Vector2(3f, -2f), LookInputTimeMode.Delta),
            };

            WriteProperty(controller, "MouseSensitivity", 2f);
            Invoke(controller, "SettingInputStateProvider", new Func<PlayerInputState>(() => state));
            Invoke(controller, "UpdatingController", 0.001f);

            Assert.AreEqual(6f, ReadProperty<float>(controller, "ControlYaw"), 0.0001f);
            Assert.AreEqual(4f, ReadProperty<float>(controller, "ControlPitch"), 0.0001f);
        }

        [Test]
        public void PlayerControllerUpdate_AppliesPresentationLookSensitivityMultiplier()
        {
            Type controllerType = RequireRuntimeType("CGame.PlayerController");
            object controller = Activator.CreateInstance(controllerType);
            PlayerInputState state = new PlayerInputState
            {
                LookInput = new LookInputValue(new Vector2(4f, -2f), LookInputTimeMode.Delta),
            };

            WriteProperty(controller, "MouseSensitivity", 2f);
            Invoke(controller, "SettingLookSensitivityMultiplier", 0.5f);
            Invoke(controller, "SettingInputStateProvider", new Func<PlayerInputState>(() => state));
            Invoke(controller, "UpdatingController", 0.016f);

            Assert.AreEqual(4f, ReadProperty<float>(controller, "ControlYaw"), 0.0001f);
            Assert.AreEqual(2f, ReadProperty<float>(controller, "ControlPitch"), 0.0001f);
            Assert.AreEqual(0.5f, ReadProperty<float>(controller, "LookSensitivityMultiplier"), 0.0001f);

            TargetInvocationException invalidMultiplier = Assert.Throws<TargetInvocationException>(() =>
                Invoke(controller, "SettingLookSensitivityMultiplier", -0.1f));
            Assert.IsInstanceOf<ArgumentOutOfRangeException>(invalidMultiplier.InnerException);
        }

        /// <summary>
        /// 验证 PlayerController 把 Stick Rate 按每秒角速度和 deltaTime 转换为当帧旋转。
        /// </summary>
        [Test]
        public void PlayerControllerUpdate_AppliesStickLookUsingDeltaTime()
        {
            Type controllerType = RequireRuntimeType("CGame.PlayerController");
            object controller = Activator.CreateInstance(controllerType);
            PlayerInputState state = new PlayerInputState
            {
                LookInput = new LookInputValue(new Vector2(1f, -0.5f), LookInputTimeMode.Rate),
            };

            WriteProperty(controller, "StickDegreesPerSecond", 120f);
            Invoke(controller, "SettingInputStateProvider", new Func<PlayerInputState>(() => state));
            Invoke(controller, "UpdatingController", 0.25f);

            Assert.AreEqual(30f, ReadProperty<float>(controller, "ControlYaw"), 0.0001f);
            Assert.AreEqual(15f, ReadProperty<float>(controller, "ControlPitch"), 0.0001f);
        }

        /// <summary>
        /// 验证移动坐标系只使用 Aim Yaw，改变 Pitch 不会缩短或污染水平移动意图。
        /// </summary>
        [Test]
        public void PlayerControllerMovement_UsesYawOnlyAtEveryPitch()
        {
            object pawn = Activator.CreateInstance(RequireRuntimeType("CGame.Pawn"));
            object controller = Activator.CreateInstance(RequireRuntimeType("CGame.PlayerController"));
            PlayerInputState state = new PlayerInputState { MoveInput = Vector2.up };

            Invoke(controller, "PossessingPawn", pawn);
            Invoke(controller, "SettingInputStateProvider", new Func<PlayerInputState>(() => state));

            Invoke(controller, "SettingControlRotation", Quaternion.Euler(0f, 45f, 0f));
            Invoke(controller, "UpdatingController", 0.016f);
            Vector3 levelMovement = InvokeResult<Vector3>(pawn, "PeekingMovementInput");

            Invoke(controller, "SettingControlRotation", Quaternion.Euler(60f, 45f, 0f));
            Invoke(controller, "UpdatingController", 0.016f);
            Vector3 pitchedMovement = InvokeResult<Vector3>(pawn, "PeekingMovementInput");

            Assert.AreEqual(0f, levelMovement.y, 0.0001f);
            Assert.AreEqual(0f, pitchedMovement.y, 0.0001f);
            Assert.AreEqual(levelMovement.magnitude, pitchedMovement.magnitude, 0.0001f);
            Assert.Less(Vector3.Angle(levelMovement, pitchedMovement), 0.001f);
        }

        /// <summary>
        /// 验证 MovementComp 让角色根朝 Aim Yaw，即使没有移动输入也不改用移动方向。
        /// </summary>
        [Test]
        public void MovementRotation_FollowsAimYawWithoutMovementInput()
        {
            object pawn = Activator.CreateInstance(RequireRuntimeType("CGame.Pawn"));
            object movement = Activator.CreateInstance(RequireRuntimeType("CGame.MovementComp"));
            Invoke(pawn, "RegisteringComponent", movement);
            Invoke(pawn, "ApplyingControlRotation", Quaternion.Euler(60f, 90f, 0f));

            object[] arguments = { Quaternion.identity, 1f };
            MethodInfo updateRotation = movement.GetType().GetMethod("UpdateRotation");
            Assert.IsNotNull(updateRotation);
            updateRotation.Invoke(movement, arguments);

            Quaternion actualRotation = (Quaternion)arguments[0];
            Quaternion expectedRotation = Quaternion.Euler(0f, 90f, 0f);
            Assert.Less(Quaternion.Angle(expectedRotation, actualRotation), 0.001f);
        }

        private static Type RequireRuntimeType(string typeName)
        {
            Type type = Type.GetType($"{typeName}, Assembly-CSharp");
            Assert.IsNotNull(type, $"找不到运行时类型 {typeName}");
            return type;
        }

        private static void Invoke(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName);
            Assert.IsNotNull(method, $"找不到公开方法 {target.GetType().Name}.{methodName}");
            method.Invoke(target, arguments);
        }

        private static T ReadProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName);
            Assert.IsNotNull(property, $"找不到公开属性 {target.GetType().Name}.{propertyName}");
            return (T)property.GetValue(target);
        }

        private static T InvokeResult<T>(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName);
            Assert.IsNotNull(method, $"找不到公开方法 {target.GetType().Name}.{methodName}");
            return (T)method.Invoke(target, arguments);
        }

        private static void WriteProperty<T>(object target, string propertyName, T value)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName);
            Assert.IsNotNull(property, $"找不到公开属性 {target.GetType().Name}.{propertyName}");
            Assert.IsTrue(property.CanWrite, $"属性 {target.GetType().Name}.{propertyName} 不可写");
            property.SetValue(target, value);
        }
    }
}
