using System.Collections.Generic;

namespace CGame
{
    public class ControllerManager : IManager
    {
        private readonly List<IController> _controllers = new List<IController>();

        public override int Priority => 90;

        /// <summary>
        /// 创建并注册指定类型的控制器。
        /// </summary>
        public TController CreatingController<TController>() where TController : IController, new()
        {
            TController controller = new TController();
            RegisteringController(controller);
            return controller;
        }

        /// <summary>
        /// 注册控制器实例。
        /// </summary>
        public void RegisteringController(IController controller)
        {
            if (controller == null || _controllers.Contains(controller))
            {
                return;
            }

            _controllers.Add(controller);
        }

        /// <summary>
        /// 注销控制器实例。
        /// </summary>
        public void UnregisteringController(IController controller)
        {
            if (!_controllers.Remove(controller))
            {
                return;
            }

            if (controller is Controller typedController)
            {
                typedController.UnpossessingPawn();
            }
        }

        /// <summary>
        /// 获取第一个指定类型的控制器。
        /// </summary>
        public TController GettingController<TController>() where TController : class, IController
        {
            for (int i = 0; i < _controllers.Count; i++)
            {
                if (_controllers[i] is TController controller)
                {
                    return controller;
                }
            }

            return null;
        }

        /// <summary>
        /// 更新所有控制器。
        /// </summary>
        public override void Update(float elapseSeconds)
        {
            for (int i = 0; i < _controllers.Count; i++)
            {
                _controllers[i].UpdatingController(elapseSeconds);
            }
        }

        /// <summary>
        /// 关闭控制器管理器并清空控制器。
        /// </summary>
        public override void Shutdown()
        {
            for (int i = 0; i < _controllers.Count; i++)
            {
                if (_controllers[i] is Controller controller)
                {
                    controller.UnpossessingPawn();
                }
            }

            _controllers.Clear();
        }
    }
}
