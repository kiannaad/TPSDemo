using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CGame
{
    public class PlayerInputContainer : InputContainer<PlayerInputState>,
        ICallbackContainer<PlayerInput.IPlayerActions>
    {
        private static readonly Dictionary<PlayerInputStateKey, PlayerAction> stateToActionMap =
            new Dictionary<PlayerInputStateKey, PlayerAction>
            {
                [PlayerInputStateKey.MoveInput] = PlayerAction.Move,
                [PlayerInputStateKey.LookInput] = PlayerAction.Look,
                [PlayerInputStateKey.FirePressed] = PlayerAction.Fire,
                [PlayerInputStateKey.FireHeld] = PlayerAction.Fire,
                [PlayerInputStateKey.JumpPressed] = PlayerAction.Jump,
                [PlayerInputStateKey.SprintHeld] = PlayerAction.Sprint,
                [PlayerInputStateKey.AimHeld] = PlayerAction.Aim,
            };

        private readonly PlayerInput input;

        /// <summary>
        /// 创建 Player 输入容器。
        /// </summary>
        public PlayerInputContainer(PlayerInput input)
        {
            this.input = input;
        }

        protected override InputActionMap ActionMap => input.Player.Get();

        /// <summary>
        /// 注册 Player 输入回调接口。
        /// </summary>
        public void AddCallbacks(PlayerInput.IPlayerActions cb) => input.Player.AddCallbacks(cb);

        /// <summary>
        /// 移除 Player 输入回调接口。
        /// </summary>
        public void RemoveCallbacks(PlayerInput.IPlayerActions cb) => input.Player.RemoveCallbacks(cb);

        /// <summary>
        /// 根据 Player 输入状态语义查找对应的底层 Action。
        /// </summary>
        protected override InputAction GetActionByState<TStateKey>(TStateKey stateKey)
        {
            if (stateKey is not PlayerInputStateKey playerStateKey)
            {
                throw new InvalidOperationException($"PlayerInputContainer 不支持状态类型 {typeof(TStateKey).Name}");
            }

            if (!stateToActionMap.TryGetValue(playerStateKey, out PlayerAction action))
            {
                throw new InvalidOperationException($"PlayerInputContainer 未配置状态映射 {playerStateKey}");
            }

            return GetAction(action.ToString());
        }

        /// <summary>
        /// 刷新 Player 输入状态快照。
        /// </summary>
        internal override void Tick()
        {
            State = new PlayerInputState
            {
                MoveInput = input.Player.Move.ReadValue<Vector2>(),
                LookInput = input.Player.Look.ReadValue<Vector2>(),
                FirePressed = input.Player.Fire.WasPressedThisFrame(),
                FireHeld = input.Player.Fire.IsPressed(),
                JumpPressed = input.Player.Jump.WasPressedThisFrame(),
                SprintHeld = input.Player.Sprint.IsPressed(),
                AimHeld = input.Player.Aim.IsPressed(),
            };
        }
    }
}
