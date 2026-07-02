using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CGame
{
    public interface IPhysicsMoverController
    {
        /// <summary>
        /// 用于告诉 CharacterPhysicsMover 此刻应处于什么位置和旋转
        /// </summary>
        void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime);
    }
}
