using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CGame
{
    public class InputRuntimeToggleTester : MonoBehaviour
    {
        private InputManager inputManager;
        private InputType currentInputType = InputType.Player;
        private bool currentMapEnabled = true;
        private bool playerJumpEnabled = true;
        private MethodInfo updateMethod;
        private MethodInfo shutdownMethod;

        /// <summary>
        /// 初始化输入调试器。
        /// </summary>
        private void Awake()
        {
            inputManager = GameManager.GetManager<InputManager>();
            updateMethod = typeof(InputManager).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            shutdownMethod = typeof(InputManager).GetMethod("Shutdown", BindingFlags.Instance | BindingFlags.NonPublic);
            Debug.Log("[InputRuntimeToggleTester] F1 Player, F2 Vehicle, J 切换 JumpPressed, M 切换当前方案, P 打印状态。");
        }

        /// <summary>
        /// 处理输入调试快捷键。
        /// </summary>
        private void Update()
        {
            updateMethod?.Invoke(inputManager, new object[] { Time.deltaTime });

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                SwitchInputType(InputType.Player);
            }

            if (keyboard.f2Key.wasPressedThisFrame)
            {
                SwitchInputType(InputType.Vehicle);
            }

            if (keyboard.jKey.wasPressedThisFrame)
            {
                TogglePlayerJump();
            }

            if (keyboard.mKey.wasPressedThisFrame)
            {
                ToggleCurrentMap();
            }

            if (keyboard.pKey.wasPressedThisFrame)
            {
                PrintCurrentState();
            }
        }

        /// <summary>
        /// 销毁时关闭输入管理器。
        /// </summary>
        private void OnDestroy()
        {
            if (inputManager != null)
            {
                shutdownMethod?.Invoke(inputManager, null);
                inputManager = null;
            }
        }

        /// <summary>
        /// 切换当前输入方案。
        /// </summary>
        private void SwitchInputType(InputType targetType)
        {
            if (currentInputType == targetType)
            {
                Debug.Log($"[InputRuntimeToggleTester] 已经处于 {targetType} 输入方案。");
                return;
            }

            inputManager.SwitchMap(currentInputType, targetType);
            currentInputType = targetType;
            currentMapEnabled = true;
            Debug.Log($"[InputRuntimeToggleTester] 切换输入方案: {currentInputType}");
        }

        /// <summary>
        /// 切换 Player JumpPressed 状态输入。
        /// </summary>
        private void TogglePlayerJump()
        {
            InputHandle handle = inputManager.GetHandle(InputType.Player);
            if (playerJumpEnabled)
            {
                handle.DisableState(PlayerInputStateKey.JumpPressed);
                playerJumpEnabled = false;
                Debug.Log("[InputRuntimeToggleTester] 禁用 Player JumpPressed。");
            }
            else
            {
                handle.EnableState(PlayerInputStateKey.JumpPressed);
                playerJumpEnabled = true;
                Debug.Log("[InputRuntimeToggleTester] 启用 Player JumpPressed。");
            }
        }

        /// <summary>
        /// 切换当前输入方案的启用状态。
        /// </summary>
        private void ToggleCurrentMap()
        {
            InputHandle handle = inputManager.GetHandle(currentInputType);
            if (currentMapEnabled)
            {
                handle.Disable();
                currentMapEnabled = false;
                Debug.Log($"[InputRuntimeToggleTester] 禁用当前输入方案: {currentInputType}");
            }
            else
            {
                handle.Enable();
                currentMapEnabled = true;
                Debug.Log($"[InputRuntimeToggleTester] 启用当前输入方案: {currentInputType}");
            }
        }

        /// <summary>
        /// 打印当前输入状态快照。
        /// </summary>
        private void PrintCurrentState()
        {
            if (currentInputType == InputType.Player)
            {
                PlayerInputState state = inputManager.GetHandle(InputType.Player).GetState<PlayerInputState>();
                Debug.Log($"[InputRuntimeToggleTester] Player Move={state.MoveInput} Look={state.LookInput} FirePressed={state.FirePressed} FireHeld={state.FireHeld} JumpPressed={state.JumpPressed} SprintHeld={state.SprintHeld} AimHeld={state.AimHeld}");
            }
            else
            {
                VehicleInputState state = inputManager.GetHandle(InputType.Vehicle).GetState<VehicleInputState>();
                Debug.Log($"[InputRuntimeToggleTester] Vehicle Steer={state.SteerInput} Throttle={state.ThrottleValue:F2} BrakeHeld={state.BrakeHeld} ExitPressed={state.ExitPressed}");
            }
        }
    }
}
