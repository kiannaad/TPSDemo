using UnityEngine;

namespace CGame
{
    /// <summary>
    /// 进入步骤：资源准备就绪后，执行登录逻辑、初始化 GameManager 等
    /// </summary>
    public class EnterStep : ILaunchStep
    {
        private bool _isDone = false;

        public void Enter()
        {
            //Debug.Log("[Launch] Entering EnterStep: Ready for login and game logic.");

            GameManager.CreateManager(typeof(CGame.InputManager));
            //Debug.Log("<color=cyan>[Game] System Ready. Showing Login Panel...</color>");
            

            _isDone = true;
        }

        public bool Update()
        {
            return _isDone;
        }

        public void Exit()
        {
            //Debug.Log("[Launch] EnterStep Completed. Game logic is now running.");
        }
    }
}
