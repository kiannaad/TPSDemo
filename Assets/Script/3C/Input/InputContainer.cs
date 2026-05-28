using System;
using UnityEngine.InputSystem;

namespace CGame
{
    public abstract class InputContainer
    {
        protected abstract InputActionMap ActionMap { get; }

        /// <summary>
        /// 启用当前输入方案。
        /// </summary>
        public void Enable() => ActionMap.Enable();

        /// <summary>
        /// 禁用当前输入方案。
        /// </summary>
        public void Disable() => ActionMap.Disable();

        /// <summary>
        /// 根据输入状态语义启用对应的底层 Action。
        /// </summary>
        public void EnableState<TStateKey>(TStateKey stateKey) where TStateKey : Enum
        {
            GetActionByState(stateKey).Enable();
        }

        /// <summary>
        /// 根据输入状态语义禁用对应的底层 Action。
        /// </summary>
        public void DisableState<TStateKey>(TStateKey stateKey) where TStateKey : Enum
        {
            GetActionByState(stateKey).Disable();
        }

        /// <summary>
        /// 根据输入状态语义注册对应 Action 的阶段回调。
        /// </summary>
        public void AddStateCallback<TStateKey>(
            TStateKey stateKey,
            InputCallbackPhase phase,
            Action<InputAction.CallbackContext> callback)
            where TStateKey : Enum
        {
            InputAction inputAction = GetActionByState(stateKey);
            switch (phase)
            {
                case InputCallbackPhase.Started:
                    inputAction.started += callback;
                    break;
                case InputCallbackPhase.Performed:
                    inputAction.performed += callback;
                    break;
                case InputCallbackPhase.Canceled:
                    inputAction.canceled += callback;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
            }
        }

        /// <summary>
        /// 根据输入状态语义移除对应 Action 的阶段回调。
        /// </summary>
        public void RemoveStateCallback<TStateKey>(
            TStateKey stateKey,
            InputCallbackPhase phase,
            Action<InputAction.CallbackContext> callback)
            where TStateKey : Enum
        {
            InputAction inputAction = GetActionByState(stateKey);
            switch (phase)
            {
                case InputCallbackPhase.Started:
                    inputAction.started -= callback;
                    break;
                case InputCallbackPhase.Performed:
                    inputAction.performed -= callback;
                    break;
                case InputCallbackPhase.Canceled:
                    inputAction.canceled -= callback;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
            }
        }

        /// <summary>
        /// 根据状态语义查找对应的底层 Action。
        /// </summary>
        protected abstract InputAction GetActionByState<TStateKey>(TStateKey stateKey) where TStateKey : Enum;

        /// <summary>
        /// 根据 Action 名称查找底层 Action。
        /// </summary>
        protected InputAction GetAction(string actionName)
        {
            return ActionMap.FindAction(actionName, throwIfNotFound: true);
        }

        /// <summary>
        /// 刷新输入状态快照。
        /// </summary>
        internal abstract void Tick();
    }

    /// <summary>
    /// 带类型化输入状态的输入容器。
    /// </summary>
    public abstract class InputContainer<TState> : InputContainer where TState : struct
    {
        public TState State { get; protected set; }
    }
}
