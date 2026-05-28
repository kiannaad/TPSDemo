using System.Collections.Generic;
using UnityEngine;

namespace CGame
{
    public class InputManager : IManager
    {
        internal override int Priority => 100;

        private PlayerInput rawInput;
        private Dictionary<InputType, InputHandle> handles;

        /// <summary>
        /// 初始化输入管理器并启用默认 Player 输入方案。
        /// </summary>
        internal override void Init()
        {
            rawInput = new PlayerInput();
            handles = new Dictionary<InputType, InputHandle>
            {
                [InputType.Player] = new InputHandle(new PlayerInputContainer(rawInput)),
                [InputType.Vehicle] = new InputHandle(new VehicleInputContainer(rawInput)),
            };

            handles[InputType.Player].Enable();
        }

        /// <summary>
        /// 每帧刷新所有输入状态快照。
        /// </summary>
        internal override void Update(float elapseSeconds)
        {
            foreach (KeyValuePair<InputType, InputHandle> kv in handles)
            {
                kv.Value.Tick();
            }
        }

        /// <summary>
        /// 获取指定输入方案的操作句柄。
        /// </summary>
        public InputHandle GetHandle(InputType type) => handles[type];

        /// <summary>
        /// 切换当前启用的输入方案。
        /// </summary>
        public void SwitchMap(InputType from, InputType to)
        {
            handles[from].Disable();
            handles[to].Enable();
        }

        /// <summary>
        /// 关闭输入管理器并释放底层输入资源。
        /// </summary>
        internal override void Shutdown()
        {
            foreach (KeyValuePair<InputType, InputHandle> kv in handles)
            {
                kv.Value.Disable();
            }

            if (Application.isPlaying)
            {
                rawInput.Dispose();
            }
            else
            {
                Object.DestroyImmediate(rawInput.asset);
            }

            handles.Clear();
        }
    }
}
