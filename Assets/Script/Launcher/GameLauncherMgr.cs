using System;
using UnityEngine;
using System.Collections.Generic;

namespace CGame
{
    public class GameLauncher : Singleton<GameLauncher>
    {
        private Queue<ILaunchStep> _stepQueue = new Queue<ILaunchStep>();
        private ILaunchStep _curStartFun = null;
        private long _startTime = 0;
        public long StartTime => _startTime;

        public void InitGameStep()
        {
            _stepQueue.Enqueue(new PreSourceStep());
            _stepQueue.Enqueue(new EnterStep());

            // 记录当前时间戳
            DateTime utcNow = DateTime.UtcNow;
            _startTime = ((DateTimeOffset)utcNow).ToUnixTimeMilliseconds();
        }

        public void ReturnLoginPanel()
        {
            _stepQueue.Clear();
            NextFun();
        }

        public void NextFun()
        {
            if (_curStartFun == null)
            {
                if (_stepQueue.Count > 0)
                {
                    var nextFun = _stepQueue.Dequeue();
                    Debug.Log(
                        $"进入{nextFun.GetType().FullName}时间: {((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds() - _startTime}");
                    nextFun.Enter();
                    _curStartFun = nextFun;
                }
            }
            else
            {
                _curStartFun.Exit();
                Debug.Log(
                    $"退出{_curStartFun.GetType().FullName}时间: {((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds() - _startTime}");
                if (_stepQueue.Count > 0)
                {
                    var nextFun = _stepQueue.Dequeue();
                    nextFun.Enter();
                    DateTime utcNow = DateTime.UtcNow;
                    Debug.Log(
                        $"进入{nextFun.GetType().FullName}时间: {((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds() - _startTime}");
                    _curStartFun = nextFun;
                }
                else
                {
                    _curStartFun = null;
                }
            }
        }

        public void Update()
        {
            if (_curStartFun != null)
            {
                // 检查步骤是否完成
                if (_curStartFun.Update())
                {
                    // 执行下一个步骤
                    NextFun();
                }
            }
        }
    }
    
    public class GameLauncherMgr : MonoBehaviour
    {
        public void Awake()
        {
            GameLauncher.Instance.InitGameStep();
            GameLauncher.Instance.NextFun();
        }

        public void Update()
        {
            GameLauncher.Instance.Update();
        }
    }
}
