using UnityEngine;
using UnityEngine.InputSystem;

namespace CGame
{
    public class InputRuntimeLogTester : MonoBehaviour
    {
        private InputManager input;

        /// <summary>
        /// 初始化输入日志测试器。
        /// </summary>
        private void Start()
        {
            input = GameManager.GetManager<InputManager>();
            input.GetHandle(InputType.Player).AddStateCallback(
                PlayerInputStateKey.MoveInput,
                InputCallbackPhase.Performed,
                context =>
                {
                    Debug.Log($"{context.ReadValue<Vector2>()} CallBack");
                });
        }

        /// <summary>
        /// 打印移动输入状态并处理调试开关。
        /// </summary>
        private void Update()
        {
            Debug.Log($"{input.GetHandle(InputType.Player).GetState<PlayerInputState>().MoveInput}");
            if (Input.GetKeyDown(KeyCode.O))
            {
                input.GetHandle(InputType.Player).DisableState(PlayerInputStateKey.MoveInput);
            }
        }
    }
}
