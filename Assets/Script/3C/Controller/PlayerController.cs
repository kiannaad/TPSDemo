using System;
using UnityEngine;

namespace CGame
{
    public class PlayerController : Controller
    {
        private Func<PlayerInputState> inputStateProvider;

        public void SettingInputStateProvider(Func<PlayerInputState> provider)
        {
            inputStateProvider = provider;
        }

        public void SettingInputHandle(InputHandle handle)
        {
            inputStateProvider = handle == null
                ? null
                : () => handle.GetState<PlayerInputState>();
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
            ControlledPawn.SubmitControlIntent(
                new CharacterControlIntent(worldDirection, inputState.JumpPressed));
        }
    }
}
