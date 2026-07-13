using System;

namespace CGame
{ 
    public abstract class IManager
    {
        public virtual int Priority
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// 固定帧更新管理器逻辑。
        /// </summary>
        public virtual void FixedUpdate(float elapseSeconds)
        {
            
        }
        
        /// <summary>
        /// 普通帧更新管理器逻辑。
        /// </summary>
        public abstract void Update(float elapseSeconds);

        /// <summary>
        /// 渲染帧后更新管理器逻辑。
        /// </summary>
        public virtual void LateUpdate(float elapseSeconds)
        {
            
        }

        /// <summary>
        /// 初始化管理器。
        /// </summary>
        public virtual void Init()
        {
        }

        /// <summary>
        /// 关闭并清理管理器。
        /// </summary>
        public virtual void Shutdown()
        {}
    }
}
