using UnityEngine;
using CGame.Animation;

namespace CGame
{
    public class CharacterTestStep : ILaunchStep
    {
        private const string RuntimeRootName = "[CharacterTestRuntime]";

        private GameObject runtimeRoot;
        private CharacterRuntime runtimeCharacter;

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

            CharacterDefinition definition = Resources.Load<CharacterDefinition>("CharacterDefinition");
            if (definition == null || !definition.IsValid)
            {
                throw new System.InvalidOperationException("CharacterDefinition is missing or invalid.");
            }

            InputHandle playerInput = inputManager.GetHandle(InputType.Player);
            var spawner = new CharacterRuntimeSpawner(pawnManager, controllerManager);
            runtimeCharacter = spawner.SpawnPlayer(new PlayerCharacterSpawnRequest(
                runtimeRoot.transform,
                definition.VisualPrefab,
                definition.AnimationConfig,
                playerInput,
                Vector3.zero,
                Quaternion.identity,
                "RuntimeCharacter"));

            Debug.Log("[CharacterTest] Runtime ready. Use WASD to move and Space to jump.");
        }

        public bool Update()
        {
            return false;
        }

        public void Exit()
        {
            runtimeCharacter?.Dispose();
            runtimeCharacter = null;
            if (runtimeRoot != null)
            {
                Object.Destroy(runtimeRoot);
                runtimeRoot = null;
            }
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
