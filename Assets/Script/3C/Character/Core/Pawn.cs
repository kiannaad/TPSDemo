using System.Collections.Generic;
using UnityEngine;

namespace CGame
{
    public class Pawn
    {
        private readonly List<IComponent> components = new List<IComponent>();
        private Controller controller;
        private Vector3 pendingMovementInput;
        private Vector3 pendingForce;
        private Vector3 pendingImpulse;
        private bool pendingJump;

        public Controller Controller => controller;
        public PawnHost Host { get; private set; }
        public Quaternion ControlRotation { get; private set; } = Quaternion.identity;

        /// <summary>
        /// 绑定 Unity 宿主对象。
        /// </summary>
        public virtual void BindingHost(PawnHost host)
        {
            Host = host;
        }

        /// <summary>
        /// 解除 Unity 宿主对象绑定。
        /// </summary>
        public virtual void UnbindingHost(PawnHost host)
        {
            if (Host == host)
            {
                Host = null;
            }
        }

        /// <summary>
        /// 设置当前控制器。
        /// </summary>
        public virtual void SettingController(Controller controller)
        {
            this.controller = controller;
        }

        /// <summary>
        /// 清理当前控制器。
        /// </summary>
        public virtual void ClearingController(Controller controller)
        {
            if (this.controller == controller)
            {
                this.controller = null;
            }
        }

        /// <summary>
        /// 应用控制器传入的控制旋转。
        /// </summary>
        public virtual void ApplyingControlRotation(Quaternion controlRotation)
        {
            ControlRotation = controlRotation;
        }

        /// <summary>
        /// 累加移动输入。
        /// </summary>
        public virtual void AddingMovementInput(Vector3 worldDirection, float scale)
        {
            if (worldDirection == Vector3.zero || Mathf.Approximately(scale, 0f))
            {
                return;
            }

            pendingMovementInput += worldDirection.normalized * scale;
        }

        /// <summary>
        /// 消费并清空当前累计移动输入。
        /// </summary>
        public Vector3 ConsumingMovementInput()
        {
            Vector3 movementInput = Vector3.ClampMagnitude(pendingMovementInput, 1f);
            pendingMovementInput = Vector3.zero;
            return movementInput;
        }

        public void AddingForce(Vector3 force)
        {
            pendingForce += force;
        }

        public Vector3 ConsumingForce()
        {
            Vector3 force = pendingForce;
            pendingForce = Vector3.zero;
            return force;
        }

        public void AddingImpulse(Vector3 impulse)
        {
            pendingImpulse += impulse;
        }

        public Vector3 ConsumingImpulse()
        {
            Vector3 impulse = pendingImpulse;
            pendingImpulse = Vector3.zero;
            return impulse;
        }

        public void AddingJumpInput()
        {
            pendingJump = true;
        }

        public bool ConsumingJumpInput()
        {
            bool jump = pendingJump;
            pendingJump = false;
            return jump;
        }

        /// <summary>
        /// 注册 Pawn 自管理组件。
        /// </summary>
        public void RegisteringComponent(IComponent component)
        {
            if (component == null || components.Contains(component))
            {
                return;
            }

            components.Add(component);
            SortingComponents();
            component.InitializingComponent(this);
        }

        /// <summary>
        /// 注销 Pawn 自管理组件。
        /// </summary>
        public void UnregisteringComponent(IComponent component)
        {
            if (!components.Remove(component))
            {
                return;
            }

            component.ShuttingDownComponent();
        }

        /// <summary>
        /// 更新 Pawn 普通帧逻辑。
        /// </summary>
        public virtual void UpdatingPawn(float elapseSeconds)
        {
            for (int i = 0; i < components.Count; i++)
            {
                components[i].UpdatingComponent(elapseSeconds);
            }
        }

        /// <summary>
        /// 更新 Pawn 固定帧逻辑。
        /// </summary>
        public virtual void FixedUpdatingPawn(float elapseSeconds)
        {
            for (int i = 0; i < components.Count; i++)
            {
                components[i].FixedUpdatingComponent(elapseSeconds);
            }
        }

        /// <summary>
        /// 更新 Pawn 渲染后逻辑。
        /// </summary>
        public virtual void LateUpdatingPawn(float elapseSeconds)
        {
            for (int i = 0; i < components.Count; i++)
            {
                components[i].LateUpdatingComponent(elapseSeconds);
            }
        }

        /// <summary>
        /// 关闭 Pawn 并清理全部组件。
        /// </summary>
        public virtual void ShuttingDownPawn()
        {
            for (int i = 0; i < components.Count; i++)
            {
                components[i].ShuttingDownComponent();
            }

            components.Clear();
            controller = null;
            Host = null;
            pendingMovementInput = Vector3.zero;
            pendingForce = Vector3.zero;
            pendingImpulse = Vector3.zero;
            pendingJump = false;
        }

        /// <summary>
        /// 按组件优先级排序。
        /// </summary>
        private void SortingComponents()
        {
            components.Sort((left, right) => right.Priority.CompareTo(left.Priority));
        }
    }
}
