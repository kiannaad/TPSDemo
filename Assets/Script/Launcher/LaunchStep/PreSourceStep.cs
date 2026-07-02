using UnityEngine;
using System.Threading.Tasks;

namespace CGame
{
    /// <summary>
    /// 资源初始化步骤：负责初始化 YooAsset 和基础资源环境
    /// </summary>
    public class PreSourceStep : ILaunchStep
    {
        private bool _isDone = false;

        public async void Enter()
        {
           // Debug.Log("[Launch] Entering PreSourceStep: Initializing ResourceManager...");
            
            // 调用我们之前落地的 ResourceManager 初始化
            await ResourceManager.Instance.InitializeAsync("DefaultPackage");
            
            // 可以在这里添加额外的预加载逻辑
            // await ResourceManager.Instance.PreloadAssetsAsync(new string[] { "Config", "BaseUI" });

            _isDone = true;
        }

        public bool Update()
        {
            // 当资源系统初始化完成后，Update 返回 true 自动进入下一步
            return _isDone;
        }

        public void Exit()
        {
        }
    }
}
