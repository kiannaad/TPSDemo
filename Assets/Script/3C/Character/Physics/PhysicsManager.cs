using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CGame
{
    /// <summary>
    /// 负责管理 CharacterPhysicsMotor 与 CharacterPhysicsMover 模拟的系统
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PhysicsManager : IManager
    {
        public override int Priority => 70;

        public override void Init()
        {
            _instance = this;
            if (Settings == null)
            {
                Settings = ScriptableObject.CreateInstance<CharacterPhysicsSettings>();
            }

            SetCharacterMotorsCapacity(Settings.MotorsListInitialCapacity);
            SetCharacterPhysicsMoversCapacity(Settings.MoversListInitialCapacity);
        }

        public override void Shutdown()
        {
            CharacterMotors.Clear();
            CharacterPhysicsMovers.Clear();
            if (Settings != null)
            {
                UnityEngine.Object.Destroy(Settings);
                Settings = null;
            }

            _instance = null;
        }

        // 全局唯一系统实例；Motor 和 Mover 会通过 EnsureCreation 确保它存在。
        private static PhysicsManager _instance;

        // 当前场景中所有启用的角色 Motor，模拟时按列表顺序更新。
        public static List<CharacterPhysicsMotor> CharacterMotors = new List<CharacterPhysicsMotor>();
        // 当前场景中所有启用的运动学平台/移动刚体，模拟时先于角色计算速度。
        public static List<CharacterPhysicsMover> CharacterPhysicsMovers = new List<CharacterPhysicsMover>();

        // 最近一次物理模拟开始的 Time.time，用来在 LateUpdate 中计算渲染插值比例。
        private static float _lastCustomInterpolationStartTime = -1f;
        // 最近一次物理模拟使用的固定步长，用来把真实时间转换为 0 到 1 的插值比例。
        private static float _lastCustomInterpolationDeltaTime = -1f;

        // 全局设置；如果没有外部设置，EnsureCreation 会创建默认 ScriptableObject。
        public static CharacterPhysicsSettings Settings;

        /// <summary>
        /// 获取当前的 CharacterPhysicsSystem 实例（如果存在）
        /// </summary>
        /// <returns></returns>
        public static PhysicsManager GetInstance()
        {
            return _instance;
        }

        /// <summary>
        /// 设置角色 Motor 列表的最大容量，以减少添加角色时的额外分配
        /// </summary>
        /// <param name="capacity"></param>
        public static void SetCharacterMotorsCapacity(int capacity)
        {
            // 容量不能小于当前元素数，否则 List 会抛异常或触发不必要复制。
            if (capacity < CharacterMotors.Count)
            {
                capacity = CharacterMotors.Count;
            }
            // 预分配容量主要是为了减少运行中添加角色时的 GC 和数组扩容。
            CharacterMotors.Capacity = capacity;
        }

        /// <summary>
        /// 向系统注册一个 CharacterPhysicsMotor
        /// </summary>
        public static void RegisterCharacterMotor(CharacterPhysicsMotor motor)
        {
            // Motor 生命周期由自身 OnEnable/OnDisable 管理，这里只维护模拟列表。
            CharacterMotors.Add(motor);
        }

        /// <summary>
        /// 从系统中注销一个 CharacterPhysicsMotor
        /// </summary>
        public static void UnregisterCharacterMotor(CharacterPhysicsMotor motor)
        {
            // 移除后该 Motor 将不再参与 FixedUpdate 物理模拟。
            CharacterMotors.Remove(motor);
        }

        /// <summary>
        /// 设置 CharacterPhysicsMover 列表的最大容量，以减少添加移动平台时的额外分配
        /// </summary>
        /// <param name="capacity"></param>
        public static void SetCharacterPhysicsMoversCapacity(int capacity)
        {
            // 容量至少要能容纳当前已注册的移动平台。
            if (capacity < CharacterPhysicsMovers.Count)
            {
                capacity = CharacterPhysicsMovers.Count;
            }
            // 提前扩容，避免大量平台动态注册时产生额外分配。
            CharacterPhysicsMovers.Capacity = capacity;
        }

        /// <summary>
        /// 向系统注册一个 CharacterPhysicsMover
        /// </summary>
        public static void RegisterCharacterPhysicsMover(CharacterPhysicsMover mover)
        {
            // Mover 生命周期同样由自身 OnEnable/OnDisable 管理。
            CharacterPhysicsMovers.Add(mover);

            // 插件自己在 LateUpdate 中统一处理插值，禁用 Rigidbody 内置插值避免双重插值。
            mover.Rigidbody.interpolation = RigidbodyInterpolation.None;
        }

        /// <summary>
        /// 从系统中注销一个 CharacterPhysicsMover
        /// </summary>
        public static void UnregisterCharacterPhysicsMover(CharacterPhysicsMover mover)
        {
            // 移除后该移动平台不再计算平台速度，也不再参与角色附着速度计算。
            CharacterPhysicsMovers.Remove(mover);
        }

        /// <summary>
        /// 普通帧更新；当前物理系统只在固定帧和渲染后处理。
        /// </summary>
        public override void Update(float elapseSeconds)
        {
        }

        /// <summary>
        /// 固定帧驱动角色物理模拟。
        /// </summary>
        public override void FixedUpdate(float elapseSeconds)
        {
            // 自动模拟开启时，由 Unity 固定物理步驱动整套 KCC 模拟。
            if (Settings.AutoSimulation)
            {
                // 这里使用本次固定步长；每一步物理都以同样的时间尺度推进。
                float deltaTime = elapseSeconds;

                // 模拟前记录插值起点，并把 Transform/Rigidbody 对齐到内部物理姿态。
                if (Settings.Interpolate)
                {
                    PreSimulationInterpolationUpdate(deltaTime);
                }

                // 执行一次完整物理模拟：平台速度 -> 角色 Phase1 -> 平台落位 -> 角色 Phase2。
                Simulate(deltaTime, CharacterMotors, CharacterPhysicsMovers);

                // 模拟后把显示姿态放回起点，等待 LateUpdate 按渲染时间平滑插值。
                if (Settings.Interpolate)
                {
                    PostSimulationInterpolationUpdate(deltaTime);
                }
            }
        }

        /// <summary>
        /// 渲染帧后执行自定义插值。
        /// </summary>
        public override void LateUpdate(float elapseSeconds)
        {
            // 渲染帧可能比物理帧更频繁，LateUpdate 用于在两个物理结果之间补中间画面。
            if (Settings.Interpolate)
            {
                CustomInterpolationUpdate();
            }
        }

        /// <summary>
        /// 记录 CharacterPhysicsMotor 与 CharacterPhysicsMover 的插值起点
        /// </summary>
        public static void PreSimulationInterpolationUpdate(float deltaTime)
        {
            // 保存角色模拟前姿态，并把 Transform 放到当前物理姿态，保证查询位置正确。
            for (int i = 0; i < CharacterMotors.Count; i++)
            {
                CharacterPhysicsMotor motor = CharacterMotors[i];

                // 记录物理步起点，后续 LateUpdate 会从这个位置插值到 TransientPosition。
                motor.InitialTickPosition = motor.TransientPosition;
                motor.InitialTickRotation = motor.TransientRotation;

                // 物理查询依赖 Transform 姿态，模拟前必须和 Motor 内部物理状态对齐。
                motor.Transform.SetPositionAndRotation(motor.TransientPosition, motor.TransientRotation);
            }

            // 保存平台模拟前姿态，并让 Transform/Rigidbody 与内部物理状态一致。
            for (int i = 0; i < CharacterPhysicsMovers.Count; i++)
            {
                CharacterPhysicsMover mover = CharacterPhysicsMovers[i];

                // 记录平台物理步起点，给后续显示插值使用。
                mover.InitialTickPosition = mover.TransientPosition;
                mover.InitialTickRotation = mover.TransientRotation;

                // 平台的 Transform 和 Rigidbody 都要对齐，否则角色查询到的平台位置会不一致。
                mover.Transform.SetPositionAndRotation(mover.TransientPosition, mover.TransientRotation);
                mover.Rigidbody.position = mover.TransientPosition;
                mover.Rigidbody.rotation = mover.TransientRotation;
            }
        }

        /// <summary>
        /// 驱动角色和移动平台进行一次模拟
        /// </summary>
        public static void Simulate(float deltaTime, List<CharacterPhysicsMotor> motors, List<CharacterPhysicsMover> movers)
        {
            int characterMotorsCount = motors.Count;
            int physicsMoversCount = movers.Count;

#pragma warning disable 0162
            // 第一步：先让所有 CharacterPhysicsMover 根据业务控制器计算本物理步目标位姿和速度。
            for (int i = 0; i < physicsMoversCount; i++)
            {
                movers[i].VelocityUpdate(deltaTime);
            }

            // 第二步：角色 Phase1 在平台真正落位前执行，用于初始化、去穿插、探地和读取平台速度。
            for (int i = 0; i < characterMotorsCount; i++)
            {
                motors[i].UpdatePhase1(deltaTime);
            }

            // 第三步：把平台应用到目标位置，让角色 Phase2 能基于平台最终位置解决重叠和位移。
            for (int i = 0; i < physicsMoversCount; i++)
            {
                CharacterPhysicsMover mover = movers[i];

                // Transform 给场景查询和显示使用。
                mover.Transform.SetPositionAndRotation(mover.TransientPosition, mover.TransientRotation);
                // Rigidbody 给 Unity Physics 查询和刚体交互使用。
                mover.Rigidbody.position = mover.TransientPosition;
                mover.Rigidbody.rotation = mover.TransientRotation;
            }

            // 第四步：角色 Phase2 解旋转、业务速度、碰撞移动、刚体推动，并得到最终内部物理姿态。
            for (int i = 0; i < characterMotorsCount; i++)
            {
                CharacterPhysicsMotor motor = motors[i];

                // Phase2 会调用 ICharacterPhysicsController.UpdateRotation/UpdateVelocity，并执行移动求解。
                motor.UpdatePhase2(deltaTime);

                // 模拟结束后，把 Transform 放到本物理步终点；如果开启插值，后面会临时放回起点。
                motor.Transform.SetPositionAndRotation(motor.TransientPosition, motor.TransientRotation);
            }
#pragma warning restore 0162
        }

        /// <summary>
        /// 为 CharacterPhysicsMotor 与 CharacterPhysicsMover 启动插值
        /// </summary>
        public static void PostSimulationInterpolationUpdate(float deltaTime)
        {
            // 记录插值时间窗口：LateUpdate 用当前 Time.time 与该时间比较得到插值比例。
            _lastCustomInterpolationStartTime = Time.time;
            _lastCustomInterpolationDeltaTime = deltaTime;

            // 把角色显示姿态恢复到物理步起点，之后每个渲染帧逐步插到终点。
            for (int i = 0; i < CharacterMotors.Count; i++)
            {
                CharacterPhysicsMotor motor = CharacterMotors[i];

                // 注意：Motor 内部的 TransientPosition 仍然是模拟终点，这里只回退 Transform 显示姿态。
                motor.Transform.SetPositionAndRotation(motor.InitialTickPosition, motor.InitialTickRotation);
            }

            // 对平台做同样的插值准备，同时按 MoveWithPhysics 决定是否走 Rigidbody.MovePosition。
            for (int i = 0; i < CharacterPhysicsMovers.Count; i++)
            {
                CharacterPhysicsMover mover = CharacterPhysicsMovers[i];

                if (mover.MoveWithPhysics)
                {
                    // 先把 Rigidbody 回到物理步起点。
                    mover.Rigidbody.position = mover.InitialTickPosition;
                    mover.Rigidbody.rotation = mover.InitialTickRotation;

                    // 再用 MovePosition/MoveRotation 告诉 Unity 物理系统本步目标姿态。
                    mover.Rigidbody.MovePosition(mover.TransientPosition);
                    mover.Rigidbody.MoveRotation(mover.TransientRotation);
                }
                else
                {
                    // 不走物理移动时，直接把 Rigidbody 放到模拟终点。
                    mover.Rigidbody.position = (mover.TransientPosition);
                    mover.Rigidbody.rotation = (mover.TransientRotation);
                }
            }
        }

        /// <summary>
        /// 处理逐帧插值
        /// </summary>
        private static void CustomInterpolationUpdate()
        {
            // 计算当前渲染帧处于上一个固定物理步的哪个比例；范围被限制在 0 到 1。
            float interpolationFactor = Mathf.Clamp01((Time.time - _lastCustomInterpolationStartTime) / _lastCustomInterpolationDeltaTime);

            // 处理角色插值：只改 Transform 显示姿态，不改变 Motor 的真实物理终点。
            for (int i = 0; i < CharacterMotors.Count; i++)
            {
                CharacterPhysicsMotor motor = CharacterMotors[i];

                // 位置线性插值，旋转球面插值，让渲染画面在两个物理结果之间平滑过渡。
                motor.Transform.SetPositionAndRotation(
                    Vector3.Lerp(motor.InitialTickPosition, motor.TransientPosition, interpolationFactor),
                    Quaternion.Slerp(motor.InitialTickRotation, motor.TransientRotation, interpolationFactor));
            }

            // 处理 CharacterPhysicsMover 插值：平台显示也要和角色使用同一套插值，否则角色脚下平台会抖。
            for (int i = 0; i < CharacterPhysicsMovers.Count; i++)
            {
                CharacterPhysicsMover mover = CharacterPhysicsMovers[i];

                // 把平台显示姿态插值到当前渲染时刻。
                mover.Transform.SetPositionAndRotation(
                    Vector3.Lerp(mover.InitialTickPosition, mover.TransientPosition, interpolationFactor),
                    Quaternion.Slerp(mover.InitialTickRotation, mover.TransientRotation, interpolationFactor));

                // 记录插值产生的显示位移，供相机或附加显示逻辑感知平台这一渲染帧的变化。
                Vector3 newPos = mover.Transform.position;
                Quaternion newRot = mover.Transform.rotation;
                mover.PositionDeltaFromInterpolation = newPos - mover.LatestInterpolationPosition;
                mover.RotationDeltaFromInterpolation = Quaternion.Inverse(mover.LatestInterpolationRotation) * newRot;
                // 保存本渲染帧姿态，作为下一次 LateUpdate 的增量基准。
                mover.LatestInterpolationPosition = newPos;
                mover.LatestInterpolationRotation = newRot;
            }
        }
    }
}
