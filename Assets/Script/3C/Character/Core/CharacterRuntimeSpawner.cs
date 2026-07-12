using System;
using CGame.Animation;
using UnityEngine;

namespace CGame
{
    /// <summary>
    /// 集中装配本地玩家角色，并在任一步失败时回滚已创建资源。
    /// </summary>
    public sealed class CharacterRuntimeSpawner
    {
        private readonly PawnManager pawnManager;
        private readonly ControllerManager controllerManager;

        public CharacterRuntimeSpawner(PawnManager pawnManager, ControllerManager controllerManager)
        {
            this.pawnManager = pawnManager ?? throw new ArgumentNullException(nameof(pawnManager));
            this.controllerManager = controllerManager ?? throw new ArgumentNullException(nameof(controllerManager));
        }

        public CharacterRuntime SpawnPlayer(in PlayerCharacterSpawnRequest request)
        {
            if (request.AnimationConfig == null || !request.AnimationConfig.IsValid)
            {
                throw new ArgumentException("A valid character animation config is required.", nameof(request));
            }

            if (request.Input == null)
            {
                throw new ArgumentNullException(nameof(request), "Player input is required.");
            }

            GameObject root = null;
            Character character = null;
            PawnHost pawnHost = null;
            CharacterPhysicsMotor motor = null;
            PlayerController controller = null;
            bool pawnRegistered = false;

            try
            {
                root = new GameObject(string.IsNullOrWhiteSpace(request.Name) ? "RuntimeCharacter" : request.Name);
                root.SetActive(false);
                root.transform.SetParent(request.Parent);
                root.transform.SetLocalPositionAndRotation(request.Position, request.Rotation);

                Animator animator = CreatingVisual(root.transform, request.AnimationConfig);
                pawnHost = root.AddComponent<PawnHost>();
                motor = root.AddComponent<CharacterPhysicsMotor>();
                character = new Character();
                var movement = new MovementComp();
                movement.BindingMotor(motor);

                pawnHost.MeshRoot = animator.transform;
                pawnHost.Animator = animator;
                pawnHost.BindingPawn(character);
                character.RegisteringComponent(movement);
                character.RegisteringComponent(new CharacterAnimationComponent(animator, motor, movement, request.AnimationConfig));
                motor.CharacterController = movement;

                pawnManager.RegisteringPawn(character);
                pawnRegistered = true;
                controller = controllerManager.CreatingController<PlayerController>();
                controller.SettingInputHandle(request.Input);
                controller.PossessingPawn(character);

                root.SetActive(true);
                return new CharacterRuntime(root, character, pawnHost, motor, controller, pawnManager, controllerManager);
            }
            catch
            {
                controller?.SettingInputHandle(null);
                controllerManager.UnregisteringController(controller);
                if (pawnRegistered)
                {
                    pawnManager.UnregisteringPawn(character);
                }

                pawnHost?.UnbindingPawn();
                if (motor != null)
                {
                    motor.CharacterController = null;
                }

                DestroyRoot(root);
                throw;
            }
        }

        private static Animator CreatingVisual(Transform parent, CharacterAnimationConfig animationConfig)
        {
            GameObject visual = UnityEngine.Object.Instantiate(animationConfig.CharacterPrefab, parent);
            visual.name = "CharacterVisual";
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>())
            {
                UnityEngine.Object.Destroy(collider);
            }

            Animator animator = visual.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                throw new InvalidOperationException("Configured character prefab does not contain an Animator.");
            }

            animator.applyRootMotion = false;
            return animator;
        }

        private static void DestroyRoot(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(root);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
