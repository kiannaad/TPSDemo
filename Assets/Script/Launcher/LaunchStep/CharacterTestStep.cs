using UnityEngine;

namespace CGame
{
    public class CharacterTestStep : ILaunchStep
    {
        private const string RuntimeRootName = "[CharacterTestRuntime]";

        private GameObject runtimeRoot;

        public void Enter()
        {
            _ = GameManager.Instance;
            InputManager inputManager = GameManager.GetManager<InputManager>();
            ControllerManager controllerManager = GameManager.GetManager<ControllerManager>();
            PawnManager pawnManager = GameManager.GetManager<PawnManager>();
            GameManager.GetManager<PhysicsManager>();

            runtimeRoot = new GameObject(RuntimeRootName);
            CreatingCamera(runtimeRoot.transform);
            CreatingLight(runtimeRoot.transform);
            CreatingGround(runtimeRoot.transform);

            GameObject characterObject = CreatingCharacter(runtimeRoot.transform);
            PawnHost pawnHost = characterObject.AddComponent<PawnHost>();
            CharacterPhysicsMotor motor = characterObject.AddComponent<CharacterPhysicsMotor>();

            Character pawn = new Character();
            MovementComp movementComp = new MovementComp();
            movementComp.BindingMotor(motor);
            pawn.RegisteringComponent(movementComp);
            pawnHost.BindingPawn(pawn);
            pawnManager.RegisteringPawn(pawn);
            motor.CharacterController = movementComp;

            PlayerController playerController = controllerManager.CreatingController<PlayerController>();
            InputHandle playerInput = inputManager.GetHandle(InputType.Player);
            playerController.SettingInputHandle(playerInput);
            playerController.PossessingPawn(pawn);

            Debug.Log("[CharacterTest] Runtime ready. Use WASD to move and Space to jump.");
        }

        public bool Update()
        {
            return false;
        }

        public void Exit()
        {
            if (runtimeRoot != null)
            {
                Object.Destroy(runtimeRoot);
                runtimeRoot = null;
            }
        }

        private static GameObject CreatingCharacter(Transform parent)
        {
            GameObject character = new GameObject("RuntimeCharacter");
            character.transform.SetParent(parent);

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(character.transform);
            visual.transform.localPosition = Vector3.up;
            Object.Destroy(visual.GetComponent<Collider>());

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
