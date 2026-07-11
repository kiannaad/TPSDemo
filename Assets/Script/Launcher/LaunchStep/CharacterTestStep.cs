using UnityEngine;
using CGame.Animation;

namespace CGame
{
    public class CharacterTestStep : ILaunchStep
    {
        private const string RuntimeRootName = "[CharacterTestRuntime]";

        private GameObject runtimeRoot;
        private Character runtimeCharacter;
        private PawnManager pawnManager;

        public void Enter()
        {
            _ = GameManager.Instance;
            InputManager inputManager = GameManager.GetManager<InputManager>();
            ControllerManager controllerManager = GameManager.GetManager<ControllerManager>();
            pawnManager = GameManager.GetManager<PawnManager>();
            GameManager.GetManager<PhysicsManager>();

            runtimeRoot = new GameObject(RuntimeRootName);
            CreatingCamera(runtimeRoot.transform);
            CreatingLight(runtimeRoot.transform);
            CreatingGround(runtimeRoot.transform);

            CharacterAnimationConfig animationConfig = Resources.Load<CharacterAnimationConfig>("CharacterAnimationConfig");
            if (animationConfig == null || !animationConfig.IsValid)
            {
                throw new System.InvalidOperationException("CharacterAnimationConfig is missing or invalid.");
            }

            GameObject characterObject = CreatingCharacter(runtimeRoot.transform, animationConfig, out Animator animator);
            PawnHost pawnHost = characterObject.AddComponent<PawnHost>();
            CharacterPhysicsMotor motor = characterObject.AddComponent<CharacterPhysicsMotor>();

            runtimeCharacter = new Character();
            MovementComp movementComp = new MovementComp();
            movementComp.BindingMotor(motor);
            pawnHost.MeshRoot = animator.transform;
            pawnHost.Animator = animator;
            pawnHost.BindingPawn(runtimeCharacter);
            runtimeCharacter.RegisteringComponent(movementComp);
            runtimeCharacter.RegisteringComponent(new CharacterAnimationComponent(animator, motor, movementComp, animationConfig));
            pawnManager.RegisteringPawn(runtimeCharacter);
            motor.CharacterController = movementComp;

            PlayerController playerController = controllerManager.CreatingController<PlayerController>();
            InputHandle playerInput = inputManager.GetHandle(InputType.Player);
            playerController.SettingInputHandle(playerInput);
            playerController.PossessingPawn(runtimeCharacter);

            Debug.Log("[CharacterTest] Runtime ready. Use WASD to move and Space to jump.");
        }

        public bool Update()
        {
            return false;
        }

        public void Exit()
        {
            if (runtimeCharacter != null && pawnManager != null)
            {
                pawnManager.UnregisteringPawn(runtimeCharacter);
                runtimeCharacter = null;
            }

            pawnManager = null;
            if (runtimeRoot != null)
            {
                Object.Destroy(runtimeRoot);
                runtimeRoot = null;
            }
        }

        private static GameObject CreatingCharacter(
            Transform parent,
            CharacterAnimationConfig animationConfig,
            out Animator animator)
        {
            GameObject character = new GameObject("RuntimeCharacter");
            character.transform.SetParent(parent);

            GameObject visual = Object.Instantiate(animationConfig.CharacterPrefab);
            visual.name = "CharacterVisual";
            visual.transform.SetParent(character.transform);
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>())
            {
                Object.Destroy(collider);
            }

            animator = visual.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                throw new System.InvalidOperationException("Configured character prefab does not contain an Animator.");
            }

            animator.applyRootMotion = false;

            return character;
        }

        private static void CreatingGround(Transform parent)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(parent);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
        }

        private static void CreatingCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent);
            cameraObject.transform.position = new Vector3(0f, 8f, -10f);
            cameraObject.transform.rotation = Quaternion.Euler(25f, 0f, 0f);
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        private static void CreatingLight(Transform parent)
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(parent);
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
        }
    }
}
