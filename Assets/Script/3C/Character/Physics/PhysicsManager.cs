using UnityEngine;

namespace CGame
{
    [DefaultExecutionOrder(-100)]
    public sealed class PhysicsManager : IManager
    {
        private CharacterPhysicsWorld world;
        public override int Priority => 70;
        public static ICharacterPhysicsWorld CurrentWorld { get; private set; }

        public override void Init()
        {
            var settings = ScriptableObject.CreateInstance<CharacterPhysicsSettings>();
            world = new CharacterPhysicsWorld(settings);
            CurrentWorld = world;
        }

        public override void Update(float elapseSeconds) { }
        public override void FixedUpdate(float elapseSeconds) => world?.Step(elapseSeconds, Time.time);
        public override void LateUpdate(float elapseSeconds) => world?.Present(Time.time);

        public override void Shutdown()
        {
            if (CurrentWorld == world) CurrentWorld = null;
            world?.Dispose();
            world = null;
        }
    }
}
