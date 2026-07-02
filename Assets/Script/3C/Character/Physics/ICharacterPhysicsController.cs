using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CGame
{
    public interface ICharacterPhysicsController
    {
        /// <summary>
        /// 当 motor 需要知道角色此刻应有的旋转时调用
        /// </summary>
        void UpdateRotation(ref Quaternion currentRotation, float deltaTime);
        /// <summary>
        /// 当 motor 需要知道角色此刻应有的速度时调用
        /// </summary>
        void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime);
        /// <summary>
        /// 在 motor 开始任何更新前调用
        /// </summary>
        void BeforeCharacterUpdate(float deltaTime);
        /// <summary>
        /// 在 motor 完成地面探测后、但在处理 CharacterPhysicsMover/速度等逻辑前调用
        /// </summary>
        void PostGroundingUpdate(float deltaTime);
        /// <summary>
        /// 在 motor 完成整次更新后调用
        /// </summary>
        void AfterCharacterUpdate(float deltaTime);
        /// <summary>
        /// 当 motor 需要判断某个碰撞体是否应参与碰撞（或直接穿过）时调用
        /// </summary>
        bool IsColliderValidForCollisions(Collider coll);
        /// <summary>
        /// 当 motor 的地面探测命中地面时调用
        /// </summary>
        void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport);
        /// <summary>
        /// 当 motor 的移动求解检测到命中时调用
        /// </summary>
        void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport);
        /// <summary>
        /// 每次移动命中后都会调用，允许你按需要修改 HitStabilityReport
        /// </summary>
        void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport);
        /// <summary>
        /// 当角色检测到离散碰撞时调用（即不是由移动过程中的 capsule cast 产生的碰撞）
        /// </summary>
        void OnDiscreteCollisionDetected(Collider hitCollider);
    }
}
