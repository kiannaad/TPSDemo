using UnityEngine;

namespace CGame
{
    public class Controller : IController
    {
        private Pawn controlledPawn;
        private Vector3 rotationInput;

        public Pawn ControlledPawn => controlledPawn;
        public Quaternion ControlRotation { get; private set; } = Quaternion.identity;

        /// <summary>
        /// 更新控制器逻辑，并把控制旋转同步给当前 Pawn。
        /// </summary>
        public virtual void UpdatingController(float elapseSeconds)
        {
            UpdatingControlRotation();
            ApplyingControlRotationToPawn();
        }

        /// <summary>
        /// 控制指定 Pawn。
        /// </summary>
        public virtual void PossessingPawn(Pawn pawn)
        {
            if (controlledPawn == pawn)
            {
                return;
            }

            UnpossessingPawn();
            controlledPawn = pawn;
            controlledPawn?.SettingController(this);
        }

        /// <summary>
        /// 解除当前 Pawn 的控制关系。
        /// </summary>
        public virtual void UnpossessingPawn()
        {
            if (controlledPawn == null)
            {
                return;
            }

            Pawn oldPawn = controlledPawn;
            controlledPawn = null;
            oldPawn.ClearingController(this);
            oldPawn.ClearingControlIntent();
        }

        /// <summary>
        /// 设置控制旋转。
        /// </summary>
        public void SettingControlRotation(Quaternion controlRotation)
        {
            ControlRotation = controlRotation;
        }

        /// <summary>
        /// 累加俯仰旋转输入。
        /// </summary>
        public void AddingPitchInput(float value)
        {
            rotationInput.x += value;
        }

        /// <summary>
        /// 累加偏航旋转输入。
        /// </summary>
        public void AddingYawInput(float value)
        {
            rotationInput.y += value;
        }

        /// <summary>
        /// 累加翻滚旋转输入。
        /// </summary>
        public void AddingRollInput(float value)
        {
            rotationInput.z += value;
        }

        /// <summary>
        /// 按 UE 风格消费本帧旋转输入并更新控制旋转。
        /// </summary>
        protected virtual void UpdatingControlRotation()
        {
            if (rotationInput == Vector3.zero)
            {
                return;
            }

            ControlRotation = Quaternion.Euler(rotationInput) * ControlRotation;
            rotationInput = Vector3.zero;
        }

        /// <summary>
        /// 把控制旋转应用到当前 Pawn。
        /// </summary>
        protected virtual void ApplyingControlRotationToPawn()
        {
            controlledPawn?.ApplyingControlRotation(ControlRotation);
        }
    }
}
