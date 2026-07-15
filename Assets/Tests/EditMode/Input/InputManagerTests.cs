using System.Reflection;
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using CGame;

namespace CGame.Tests
{
    public class InputManagerTests : InputTestFixture
    {
        private CGame.InputManager inputManager;
        private Keyboard keyboard;
        private Mouse mouse;
        private Gamepad gamepad;

        /// <summary>
        /// 初始化隔离的 InputSystem 测试环境，并创建输入管理器。
        /// </summary>
        [SetUp]
        public override void Setup()
        {
            base.Setup();

            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            gamepad = InputSystem.AddDevice<Gamepad>();
            inputManager = new CGame.InputManager();
            InvokeManagerMethod("Init");
        }

        /// <summary>
        /// 关闭输入管理器，并还原 InputSystem 测试环境。
        /// </summary>
        [TearDown]
        public override void TearDown()
        {
            InvokeManagerMethod("Shutdown");
            inputManager = null;
            keyboard = null;
            mouse = null;
            gamepad = null;

            base.TearDown();
        }

        /// <summary>
        /// 验证禁用单个输入状态语义时，目标状态不再生效，但同一输入方案内的其它状态仍然可用。
        /// </summary>
        [Test]
        public void DisableSinglePlayerState_DisablesOnlyThatState()
        {
            CGame.InputHandle playerHandle = inputManager.GetHandle(InputType.Player);
            playerHandle.DisableState(PlayerInputStateKey.JumpPressed);

            Press(keyboard.spaceKey);
            Press(keyboard.wKey);
            UpdateInputManager();

            PlayerInputState state = playerHandle.GetState<PlayerInputState>();
            Assert.IsFalse(state.JumpPressed);
            AssertMove(state.MoveInput, 0f, 1f);
        }

        /// <summary>
        /// 验证禁用整个 Player 输入方案后，PlayerInputState 不再响应 Player Map 下的按键。
        /// </summary>
        [Test]
        public void DisableWholePlayerMap_DisablesAllPlayerActions()
        {
            InputHandle playerHandle = inputManager.GetHandle(InputType.Player);
            playerHandle.Disable();

            Press(keyboard.wKey);
            Press(keyboard.spaceKey);
            Press(keyboard.rKey);
            Press(mouse.leftButton);
            UpdateInputManager();

            PlayerInputState state = playerHandle.GetState<PlayerInputState>();
            AssertMove(state.MoveInput, 0f, 0f);
            Assert.IsFalse(state.JumpPressed);
            Assert.IsFalse(state.FirePressed);
            Assert.IsFalse(state.FireHeld);
            Assert.IsFalse(state.ReloadPressed);
        }

        /// <summary>
        /// 验证可以只注册某个输入状态的回调，且其它输入状态不会触发该回调。
        /// </summary>
        [Test]
        public void AddStateCallback_RegistersOnlySelectedState()
        {
            InputHandle playerHandle = inputManager.GetHandle(InputType.Player);
            int jumpCallbackCount = 0;

            void OnJump(InputAction.CallbackContext context)
            {
                jumpCallbackCount++;
            }

            playerHandle.AddStateCallback(PlayerInputStateKey.JumpPressed, InputCallbackPhase.Performed, OnJump);

            Press(keyboard.wKey);
            UpdateInputManager();
            Assert.AreEqual(0, jumpCallbackCount);

            Press(keyboard.spaceKey);
            UpdateInputManager();
            Assert.AreEqual(1, jumpCallbackCount);
        }

        /// <summary>
        /// 验证移除单个输入状态回调后，再次触发状态不会调用旧回调。
        /// </summary>
        [Test]
        public void RemoveStateCallback_RemovesSelectedStateCallback()
        {
            InputHandle playerHandle = inputManager.GetHandle(InputType.Player);
            int jumpCallbackCount = 0;

            void OnJump(InputAction.CallbackContext context)
            {
                jumpCallbackCount++;
            }

            playerHandle.AddStateCallback(PlayerInputStateKey.JumpPressed, InputCallbackPhase.Performed, OnJump);
            playerHandle.RemoveStateCallback(PlayerInputStateKey.JumpPressed, InputCallbackPhase.Performed, OnJump);

            Press(keyboard.spaceKey);
            UpdateInputManager();
            Assert.AreEqual(0, jumpCallbackCount);
        }

        /// <summary>
        /// 验证禁用 Player 输入方案时，会清理已注册的状态回调，重新启用后旧回调不会残留触发。
        /// </summary>
        [Test]
        public void DisablePlayerMap_RemovesRegisteredStateCallbacks()
        {
            InputHandle playerHandle = inputManager.GetHandle(InputType.Player);
            int jumpCallbackCount = 0;

            void OnJump(InputAction.CallbackContext context)
            {
                jumpCallbackCount++;
            }

            playerHandle.AddStateCallback(PlayerInputStateKey.JumpPressed, InputCallbackPhase.Performed, OnJump);
            playerHandle.Disable();
            playerHandle.Enable();

            Press(keyboard.spaceKey);
            UpdateInputManager();

            Assert.AreEqual(0, jumpCallbackCount);
        }

        /// <summary>
        /// 验证禁用 Player 输入方案时，会清理完整 Player 回调接口，重新启用后旧回调不会残留触发。
        /// </summary>
        [Test]
        public void DisablePlayerMap_RemovesRegisteredPlayerCallbacks()
        {
            InputHandle playerHandle = inputManager.GetHandle(InputType.Player);
            PlayerActionsCallbackCounter callbacks = new PlayerActionsCallbackCounter();

            playerHandle.AddCallbacks<PlayerInput.IPlayerActions>(callbacks);
            playerHandle.Disable();
            playerHandle.Enable();

            Press(keyboard.spaceKey);
            UpdateInputManager();

            Assert.AreEqual(0, callbacks.JumpCallbackCount);
        }

        /// <summary>
        /// 验证禁用 Player 输入方案时，会立即清空旧的 PlayerInputState。
        /// </summary>
        [Test]
        public void DisablePlayerMap_ClearsPlayerInputState()
        {
            InputHandle playerHandle = inputManager.GetHandle(InputType.Player);

            Press(keyboard.wKey);
            Press(keyboard.leftShiftKey);
            UpdateInputManager();

            PlayerInputState activeState = playerHandle.GetState<PlayerInputState>();
            AssertMove(activeState.MoveInput, 0f, 1f);
            Assert.IsTrue(activeState.SprintHeld);

            playerHandle.Disable();

            PlayerInputState disabledState = playerHandle.GetState<PlayerInputState>();
            AssertMove(disabledState.MoveInput, 0f, 0f);
            Assert.IsFalse(disabledState.SprintHeld);
        }

        /// <summary>
        /// 验证从 Player 切换到 Vehicle 后，Player 输入方案失效，Vehicle 输入方案生效。
        /// </summary>
        [Test]
        public void SwitchMap_DisablesPlayerAndEnablesVehicle()
        {
            inputManager.SwitchMap(InputType.Player, InputType.Vehicle);

            Press(keyboard.wKey);
            Press(keyboard.spaceKey);
            UpdateInputManager();

            PlayerInputState playerState = inputManager.GetHandle(InputType.Player).GetState<PlayerInputState>();
            VehicleInputState vehicleState = inputManager.GetHandle(InputType.Vehicle).GetState<VehicleInputState>();

            AssertMove(playerState.MoveInput, 0f, 0f);
            Assert.IsFalse(playerState.JumpPressed);
            AssertMove(vehicleState.SteerInput, 0f, 1f);
            Assert.AreEqual(1f, vehicleState.ThrottleValue, 0.0001f);
            Assert.IsTrue(vehicleState.BrakeHeld);
        }

        /// <summary>
        /// 验证 Player 输入方案下的移动、视角、开火、跳跃、冲刺、瞄准、换弹都能正常写入输入快照。
        /// </summary>
        [Test]
        public void PlayerActions_UpdatePlayerInputState()
        {
            InputHandle playerHandle = inputManager.GetHandle(InputType.Player);

            AssertPlayerMoveKey(keyboard.wKey, 0f, 1f);
            AssertPlayerMoveKey(keyboard.sKey, 0f, -1f);
            AssertPlayerMoveKey(keyboard.aKey, -1f, 0f);
            AssertPlayerMoveKey(keyboard.dKey, 1f, 0f);

            Set(mouse.delta, new Vector2(3f, -2f));
            UpdateInputManager();
            AssertMove(playerHandle.GetState<PlayerInputState>().LookInput.Value, 3f, -2f);

            Press(mouse.leftButton);
            UpdateInputManager();
            PlayerInputState fireState = playerHandle.GetState<PlayerInputState>();
            Assert.IsTrue(fireState.FirePressed);
            Assert.IsTrue(fireState.FireHeld);
            Release(mouse.leftButton);
            UpdateInputManager();

            Press(keyboard.spaceKey);
            UpdateInputManager();
            Assert.IsTrue(playerHandle.GetState<PlayerInputState>().JumpPressed);
            Release(keyboard.spaceKey);
            UpdateInputManager();

            Press(keyboard.leftShiftKey);
            UpdateInputManager();
            Assert.IsTrue(playerHandle.GetState<PlayerInputState>().SprintHeld);
            Release(keyboard.leftShiftKey);
            UpdateInputManager();

            Press(mouse.rightButton);
            UpdateInputManager();
            Assert.IsTrue(playerHandle.GetState<PlayerInputState>().AimHeld);
            Release(mouse.rightButton);
            UpdateInputManager();

            Press(keyboard.rKey);
            UpdateInputManager();
            Assert.IsTrue(playerHandle.GetState<PlayerInputState>().ReloadPressed);
            Release(keyboard.rKey);
            UpdateInputManager();
        }

        /// <summary>
        /// 验证 Pointer Delta 保留逐帧增量语义，不随 deltaTime 重复缩放。
        /// </summary>
        [Test]
        public void PointerLook_PreservesDeltaTimeMode()
        {
            Set(mouse.delta, new Vector2(12f, -6f));
            UpdateInputManager();

            LookInputValue lookInput = inputManager
                .GetHandle(InputType.Player)
                .GetState<PlayerInputState>()
                .LookInput;

            Assert.AreEqual(LookInputTimeMode.Delta, lookInput.TimeMode);
            AssertMove(lookInput.Value, 12f, -6f);
            AssertMove(lookInput.ResolveFrameDelta(1f / 30f), 12f, -6f);
            AssertMove(lookInput.ResolveFrameDelta(1f / 144f), 12f, -6f);
        }

        /// <summary>
        /// 验证 Gamepad Right Stick 保留按秒速率语义，并按 deltaTime 计算逐帧增量。
        /// </summary>
        [Test]
        public void GamepadLook_PreservesRateTimeMode()
        {
            Set(gamepad.rightStick, Vector2.right);
            UpdateInputManager();

            LookInputValue lookInput = inputManager
                .GetHandle(InputType.Player)
                .GetState<PlayerInputState>()
                .LookInput;

            Assert.AreEqual(LookInputTimeMode.Rate, lookInput.TimeMode);
            AssertMove(lookInput.Value, 1f, 0f);
            AssertMove(lookInput.ResolveFrameDelta(0.02f), 0.02f, 0f);
            AssertMove(lookInput.ResolveFrameDelta(0.01f), 0.01f, 0f);
        }

        [Test]
        public void PlayerMovementIntent_PersistsAcrossMultiplePhysicsSteps()
        {
            InputHandle playerHandle = inputManager.GetHandle(InputType.Player);
            Press(keyboard.wKey);
            Press(keyboard.dKey);
            UpdateInputManager();

            Type pawnType = RequireRuntimeType("CGame.Pawn");
            Type controllerType = RequireRuntimeType("CGame.PlayerController");
            Type movementType = RequireRuntimeType("CGame.MovementComp");
            object pawn = Activator.CreateInstance(pawnType);
            object controller = Activator.CreateInstance(controllerType);
            object movement = Activator.CreateInstance(movementType);

            pawnType.GetMethod("RegisteringComponent").Invoke(pawn, new[] { movement });
            controllerType.GetMethod("PossessingPawn").Invoke(controller, new[] { pawn });
            Func<PlayerInputState> stateProvider = () => playerHandle.GetState<PlayerInputState>();
            controllerType.GetMethod("SettingInputStateProvider").Invoke(controller, new object[] { stateProvider });
            controllerType.GetMethod("UpdatingController").Invoke(controller, new object[] { 0.1f });

            object[] velocityArguments = { Vector3.zero, 0.1f };
            movementType.GetMethod("UpdateVelocity").Invoke(movement, velocityArguments);
            Vector3 velocity = (Vector3)velocityArguments[0];

            float expectedAxisVelocity = 20f * 0.1f / Mathf.Sqrt(2f);
            Assert.AreEqual(expectedAxisVelocity, velocity.x, 0.0001f);
            Assert.AreEqual(0f, velocity.y, 0.0001f);
            Assert.AreEqual(expectedAxisVelocity, velocity.z, 0.0001f);

            object[] continuedArguments = { velocity, 0.1f };
            movementType.GetMethod("UpdateVelocity").Invoke(movement, continuedArguments);
            Vector3 continuedVelocity = (Vector3)continuedArguments[0];
            Assert.Greater(continuedVelocity.magnitude, velocity.magnitude);
        }

        [Test]
        public void CharacterControlIntent_ConsumesJumpOnceAndKeepsMovement()
        {
            Type pawnType = RequireRuntimeType("CGame.Pawn");
            Type intentType = RequireRuntimeType("CGame.CharacterControlIntent");
            object pawn = Activator.CreateInstance(pawnType);
            object intent = Activator.CreateInstance(
                intentType,
                new object[] { new Vector3(0.3f, 0f, 0.4f), true });

            pawnType.GetMethod("SubmitControlIntent").Invoke(pawn, new[] { intent });

            object firstCommand = pawnType.GetMethod("ConsumeMovementCommand").Invoke(pawn, null);
            object secondCommand = pawnType.GetMethod("ConsumeMovementCommand").Invoke(pawn, null);
            Type commandType = firstCommand.GetType();

            Assert.AreEqual(new Vector3(0.3f, 0f, 0.4f), commandType.GetProperty("MovementInput").GetValue(firstCommand));
            Assert.IsTrue((bool)commandType.GetProperty("JumpRequested").GetValue(firstCommand));
            Assert.AreEqual(new Vector3(0.3f, 0f, 0.4f), commandType.GetProperty("MovementInput").GetValue(secondCommand));
            Assert.IsFalse((bool)commandType.GetProperty("JumpRequested").GetValue(secondCommand));
        }

        /// <summary>
        /// 验证 Vehicle 输入方案下的转向按键能正常写入输入快照。
        /// </summary>
        [Test]
        public void VehicleSteerActions_UpdateVehicleInputState()
        {
            inputManager.SwitchMap(InputType.Player, InputType.Vehicle);

            AssertVehicleSteerKey(keyboard.wKey, 0f, 1f);
            AssertVehicleSteerKey(keyboard.sKey, 0f, -1f);
            AssertVehicleSteerKey(keyboard.aKey, -1f, 0f);
            AssertVehicleSteerKey(keyboard.dKey, 1f, 0f);
        }

        /// <summary>
        /// 验证 Vehicle 输入方案下的油门按键能正常写入输入快照。
        /// </summary>
        [Test]
        public void VehicleThrottleActions_UpdateVehicleInputState()
        {
            inputManager.SwitchMap(InputType.Player, InputType.Vehicle);

            Press(keyboard.wKey);
            UpdateInputManager();
            Assert.AreEqual(1f, inputManager.GetHandle(InputType.Vehicle).GetState<VehicleInputState>().ThrottleValue, 0.0001f);
            Release(keyboard.wKey);
            UpdateInputManager();

            Press(keyboard.sKey);
            UpdateInputManager();
            Assert.AreEqual(-1f, inputManager.GetHandle(InputType.Vehicle).GetState<VehicleInputState>().ThrottleValue, 0.0001f);
            Release(keyboard.sKey);
            UpdateInputManager();
        }

        /// <summary>
        /// 验证 Vehicle 输入方案下的刹车按键能正常写入输入快照。
        /// </summary>
        [Test]
        public void VehicleBrakeAction_UpdatesVehicleInputState()
        {
            inputManager.SwitchMap(InputType.Player, InputType.Vehicle);

            Press(keyboard.spaceKey);
            UpdateInputManager();

            Assert.IsTrue(inputManager.GetHandle(InputType.Vehicle).GetState<VehicleInputState>().BrakeHeld);
        }

        /// <summary>
        /// 验证 Vehicle 输入方案下的退出按键能正常写入输入快照。
        /// </summary>
        [Test]
        public void VehicleExitAction_UpdatesVehicleInputState()
        {
            inputManager.SwitchMap(InputType.Player, InputType.Vehicle);

            Press(keyboard.eKey);
            UpdateInputManager();

            Assert.IsTrue(inputManager.GetHandle(InputType.Vehicle).GetState<VehicleInputState>().ExitPressed);
        }

        /// <summary>
        /// 断言 Player 移动按键会产生预期移动向量。
        /// </summary>
        private void AssertPlayerMoveKey(KeyControl key, float expectedX, float expectedY)
        {
            Press(key);
            UpdateInputManager();
            AssertMove(inputManager.GetHandle(InputType.Player).GetState<PlayerInputState>().MoveInput, expectedX, expectedY);
            Release(key);
            UpdateInputManager();
        }

        /// <summary>
        /// 断言 Vehicle 转向按键会产生预期转向向量。
        /// </summary>
        private void AssertVehicleSteerKey(KeyControl key, float expectedX, float expectedY)
        {
            Press(key);
            UpdateInputManager();
            AssertMove(inputManager.GetHandle(InputType.Vehicle).GetState<VehicleInputState>().SteerInput, expectedX, expectedY);
            Release(key);
            UpdateInputManager();
        }

        /// <summary>
        /// 驱动输入管理器刷新输入快照。
        /// </summary>
        private void UpdateInputManager()
        {
            MethodInfo method = typeof(CGame.InputManager).GetMethod(
                "Update",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            method.Invoke(inputManager, new object[] { 0.016f });
        }

        /// <summary>
        /// 调用输入管理器的内部生命周期函数。
        /// </summary>
        private void InvokeManagerMethod(string methodName)
        {
            if (inputManager == null)
            {
                return;
            }

            MethodInfo method = typeof(CGame.InputManager).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            method.Invoke(inputManager, null);
        }

        /// <summary>
        /// 断言二维输入向量等于预期值。
        /// </summary>
        private static void AssertMove(Vector2 actual, float expectedX, float expectedY)
        {
            Assert.AreEqual(expectedX, actual.x, 0.0001f);
            Assert.AreEqual(expectedY, actual.y, 0.0001f);
        }

        private static Type RequireRuntimeType(string typeName)
        {
            Type type = Type.GetType($"{typeName}, Assembly-CSharp");
            Assert.IsNotNull(type, $"找不到运行时类型 {typeName}");
            return type;
        }

        private sealed class PlayerActionsCallbackCounter : PlayerInput.IPlayerActions
        {
            public int JumpCallbackCount;

            /// <summary>
            /// 统计移动回调次数。
            /// </summary>
            public void OnMove(InputAction.CallbackContext context)
            {
            }

            /// <summary>
            /// 统计视角回调次数。
            /// </summary>
            public void OnLook(InputAction.CallbackContext context)
            {
            }

            /// <summary>
            /// 统计开火回调次数。
            /// </summary>
            public void OnFire(InputAction.CallbackContext context)
            {
            }

            /// <summary>
            /// 统计跳跃回调次数。
            /// </summary>
            public void OnJump(InputAction.CallbackContext context)
            {
                JumpCallbackCount++;
            }

            /// <summary>
            /// 统计冲刺回调次数。
            /// </summary>
            public void OnSprint(InputAction.CallbackContext context)
            {
            }

            /// <summary>
            /// 统计瞄准回调次数。
            /// </summary>
            public void OnAim(InputAction.CallbackContext context)
            {
            }

            public void OnReload(InputAction.CallbackContext context)
            {
            }
        }
    }
}
