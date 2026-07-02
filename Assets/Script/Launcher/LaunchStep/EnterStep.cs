using UnityEngine;

namespace CGame
{
    /// <summary>
    /// 进入步骤：资源准备就绪后，执行登录逻辑、初始化 GameManager 等
    /// </summary>
    public class EnterStep : ILaunchStep
    {
        private bool isDone;

        public void Enter()
        {
            //Debug.Log("[Launch] Entering EnterStep: Ready for login and game logic.");

            _ = GameManager.Instance;
            //Debug.Log("<color=cyan>[Game] System Ready. Showing Login Panel...</color>");
            

            isDone = true;
        }

        public bool Update()
        {
            return isDone;
        }

        public void Exit()
        {
            //Debug.Log("[Launch] EnterStep Completed. Game logic is now running.");
        }
    }
}
