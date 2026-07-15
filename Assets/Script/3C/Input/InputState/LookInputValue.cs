using UnityEngine;

namespace CGame
{
    public readonly struct LookInputValue
    {
        public LookInputValue(Vector2 value, LookInputTimeMode timeMode)
        {
            Value = value;
            TimeMode = timeMode;
        }

        public Vector2 Value { get; }

        public LookInputTimeMode TimeMode { get; }

        /// <summary>
        /// 把设备输入的时间语义统一为当前帧增量，不应用灵敏度或摄像机策略。
        /// </summary>
        public Vector2 ResolveFrameDelta(float deltaTime)
        {
            return TimeMode switch
            {
                LookInputTimeMode.Delta => Value,
                LookInputTimeMode.Rate => Value * deltaTime,
                _ => Vector2.zero,
            };
        }

        public override string ToString()
        {
            return $"{Value} ({TimeMode})";
        }
    }
}
