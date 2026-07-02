using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CGame
{
    public class PlayerController : Controller
    {
        private Func<PlayerInputState> inputStateProvider;
        private InputHandle inputHandle;

        public void SettingInputStateProvider(Func<PlayerInputState> provider)
        {
            inputStateProvider = provider;
        }

        public void SettingInputHandle(InputHandle handle)
        {
            if (inputHandle != null)
            {
                inputHandle.RemoveStateCallback(
                    PlayerInputStateKey.JumpPressed,
                    InputCallbackPhase.Performed,
                    OnJumpPerformed);
            }

            inputHandle = handle;
            inputStateProvider = handle == null
                ? null
                : () => handle.GetState<PlayerInputState>();

            if (inputHandle != null)
            {
                inputHandle.AddStateCallback(
                    PlayerInputStateKey.JumpPressed,
                    InputCallbackPhase.Performed,
                    OnJumpPerformed);
            }
        }

        /// <summary>
        /// 更新玩家控制器逻辑。
        /// </summary>
        public override void UpdatingController(float elapseSeconds)
        {
            base.UpdatingController(elapseSeconds);

            if (ControlledPawn == null || inputStateProvider == null)
            {
                return;
            }

            PlayerInputState inputState = inputStateProvider();
            Vector3 localDirection = new Vector3(inputState.MoveInput.x, 0f, inputState.MoveInput.y);
            Vector3 worldDirection = ControlRotation * localDirection;
            ControlledPawn.AddingMovementInput(worldDirection, localDirection.magnitude);
            if (inputState.JumpPressed)
            {
                ControlledPawn.AddingJumpInput();
            }
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            ControlledPawn?.AddingJumpInput();
        }
    }
}
