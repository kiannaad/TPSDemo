#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;

namespace CGame
{
    /// <summary>
    /// 为角色武器动画验收提供独立快捷键，不参与正式输入或启动流程。
    /// </summary>
    public sealed class WeaponAnimationTestInput : MonoBehaviour
    {
        private const string RuntimeObjectName = "[WeaponAnimationTestInput]";
        private static readonly WeaponId TestWeaponId = new WeaponId("rifle");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateRuntimeInput()
        {
            if (FindObjectOfType<WeaponAnimationTestInput>() != null)
            {
                return;
            }

            var gameObject = new GameObject(RuntimeObjectName);
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<WeaponAnimationTestInput>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.rKey.wasPressedThisFrame)
            {
                return;
            }

            PawnHost pawnHost = FindObjectOfType<PawnHost>();
            Controller controller = pawnHost?.Pawn?.Controller;
            if (controller == null)
            {
                Debug.LogWarning("[WeaponAnimationTestInput] R ignored because no controlled Pawn is ready.");
                return;
            }

            bool changed = controller.WeaponRuntime.Snapshot.IsEquipped
                ? controller.RequestUnequipWeapon()
                : controller.RequestEquipWeapon(TestWeaponId);
            if (changed)
            {
                string state = controller.WeaponRuntime.Snapshot.IsEquipped ? "Rifle" : "Unarmed";
                Debug.Log($"[WeaponAnimationTestInput] R toggled weapon state to {state}.");
            }
        }
    }
}
#endif
