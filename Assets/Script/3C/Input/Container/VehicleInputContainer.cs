using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CGame
{
    public class VehicleInputContainer : InputContainer<VehicleInputState>,
        ICallbackContainer<PlayerInput.IVehicleActions>
    {
        private static readonly Dictionary<VehicleInputStateKey, VehicleAction> stateToActionMap =
            new Dictionary<VehicleInputStateKey, VehicleAction>
            {
                [VehicleInputStateKey.SteerInput] = VehicleAction.Steer,
                [VehicleInputStateKey.ThrottleValue] = VehicleAction.Throttle,
                [VehicleInputStateKey.BrakeHeld] = VehicleAction.Brake,
                [VehicleInputStateKey.ExitPressed] = VehicleAction.Exit,
            };

        private readonly PlayerInput input;

        /// <summary>
        /// 创建 Vehicle 输入容器。
        /// </summary>
        public VehicleInputContainer(PlayerInput input)
        {
            this.input = input;
        }

        protected override InputActionMap ActionMap => input.Vehicle.Get();

        /// <summary>
        /// 注册 Vehicle 输入回调接口。
        /// </summary>
        public void AddCallbacks(PlayerInput.IVehicleActions cb) => input.Vehicle.AddCallbacks(cb);

        /// <summary>
        /// 移除 Vehicle 输入回调接口。
        /// </summary>
        public void RemoveCallbacks(PlayerInput.IVehicleActions cb) => input.Vehicle.RemoveCallbacks(cb);

        /// <summary>
        /// 根据 Vehicle 输入状态语义查找对应的底层 Action。
        /// </summary>
        protected override InputAction GetActionByState<TStateKey>(TStateKey stateKey)
        {
            if (stateKey is not VehicleInputStateKey vehicleStateKey)
            {
                throw new InvalidOperationException($"VehicleInputContainer 不支持状态类型 {typeof(TStateKey).Name}");
            }

            if (!stateToActionMap.TryGetValue(vehicleStateKey, out VehicleAction action))
            {
                throw new InvalidOperationException($"VehicleInputContainer 未配置状态映射 {vehicleStateKey}");
            }

            return GetAction(action.ToString());
        }

        /// <summary>
        /// 刷新 Vehicle 输入状态快照。
        /// </summary>
        internal override void Tick()
        {
            State = new VehicleInputState
            {
                SteerInput = input.Vehicle.Steer.ReadValue<Vector2>(),
                ThrottleValue = input.Vehicle.Throttle.ReadValue<float>(),
                BrakeHeld = input.Vehicle.Brake.IsPressed(),
                ExitPressed = input.Vehicle.Exit.WasPressedThisFrame(),
            };
        }
    }
}
