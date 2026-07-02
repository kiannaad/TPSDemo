using System.Collections.Generic;

namespace CGame
{
    public class PawnManager : IManager
    {
        private readonly List<Pawn> pawns = new List<Pawn>();

        public override int Priority => 80;

        /// <summary>
        /// 注册 Pawn 实例。
        /// </summary>
        public void RegisteringPawn(Pawn pawn)
        {
            if (pawn == null || pawns.Contains(pawn))
            {
                return;
            }

            pawns.Add(pawn);
        }

        /// <summary>
        /// 注销 Pawn 实例。
        /// </summary>
        public void UnregisteringPawn(Pawn pawn)
        {
            if (!pawns.Remove(pawn))
            {
                return;
            }

            pawn.ShuttingDownPawn();
        }

        /// <summary>
        /// 更新所有 Pawn 的固定帧逻辑。
        /// </summary>
        public override void FixedUpdate(float elapseSeconds)
        {
            for (int i = 0; i < pawns.Count; i++)
            {
                pawns[i].FixedUpdatingPawn(elapseSeconds);
            }
        }

        /// <summary>
        /// 更新所有 Pawn 的普通帧逻辑。
        /// </summary>
        public override void Update(float elapseSeconds)
        {
            for (int i = 0; i < pawns.Count; i++)
            {
                pawns[i].UpdatingPawn(elapseSeconds);
            }
        }

        /// <summary>
        /// 更新所有 Pawn 的渲染后逻辑。
        /// </summary>
        public override void LateUpdate(float elapseSeconds)
        {
            for (int i = 0; i < pawns.Count; i++)
            {
                pawns[i].LateUpdatingPawn(elapseSeconds);
            }
        }

        /// <summary>
        /// 关闭 Pawn 管理器并清理所有 Pawn。
        /// </summary>
        public override void Shutdown()
        {
            for (int i = 0; i < pawns.Count; i++)
            {
                pawns[i].ShuttingDownPawn();
            }

            pawns.Clear();
        }
    }
}
