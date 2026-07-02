using UnityEngine;

namespace CGame
{
    public class Controller : IController
    {
        private Pawn _controlledPawn;
        private Vector3 _rotationInput;

        public Pawn ControlledPawn => _controlledPawn;
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
            if (_controlledPawn == pawn)
            {
                return;
            }

            UnpossessingPawn();
            _controlledPawn = pawn;
            _controlledPawn?.SettingController(this);
        }

        /// <summary>
        /// 解除当前 Pawn 的控制关系。
        /// </summary>
        public virtual void UnpossessingPawn()
        {
            if (_controlledPawn == null)
            {
                return;
            }

            Pawn oldPawn = _controlledPawn;
            _controlledPawn = null;
            oldPawn.ClearingController(this);
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
            _rotationInput.x += value;
        }

        /// <summary>
        /// 累加偏航旋转输入。
        /// </summary>
        public void AddingYawInput(float value)
        {
            _rotationInput.y += value;
        }

        /// <summary>
        /// 累加翻滚旋转输入。
        /// </summary>
        public void AddingRollInput(float value)
        {
            _rotationInput.z += value;
        }

        /// <summary>
        /// 按 UE 风格消费本帧旋转输入并更新控制旋转。
        /// </summary>
        protected virtual void UpdatingControlRotation()
        {
            if (_rotationInput == Vector3.zero)
            {
                return;
            }

            ControlRotation = Quaternion.Euler(_rotationInput) * ControlRotation;
            _rotationInput = Vector3.zero;
        }

        /// <summary>
        /// 把控制旋转应用到当前 Pawn。
        /// </summary>
        protected virtual void ApplyingControlRotationToPawn()
        {
            _controlledPawn?.ApplyingControlRotation(ControlRotation);
        }
    }
}
