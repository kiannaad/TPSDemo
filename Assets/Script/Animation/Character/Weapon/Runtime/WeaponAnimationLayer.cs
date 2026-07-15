using System;

namespace CGame.Animation
{
    public sealed class WeaponAnimationLayer : AnimationNodeBase, IWeaponAnimationLayer
    {
        private readonly WeaponLocomotionPoseNode locomotionPose;

        public WeaponAnimationLayer(WeaponAnimationDefinition definition, uint generation, Func<string> stateGetter)
        {
            if (definition == null || !definition.IsValid)
            {
                throw new ArgumentException("A valid weapon animation definition is required.", nameof(definition));
            }

            WeaponId = definition.WeaponId;
            Generation = generation;
            locomotionPose = new WeaponLocomotionPoseNode(definition, stateGetter);
        }

        public WeaponId WeaponId { get; }
        public uint Generation { get; }
        public bool IsPoseAvailable => locomotionPose.IsPoseAvailable;
        public bool IsDisposed { get; private set; }
        public string SelectedLocomotionState => locomotionPose.SelectedLocomotionState;

        public override void Update(AnimationGraphContext context, float deltaTime)
        {
            locomotionPose.Update(context, deltaTime);
        }

        public override AnimationPoseHandle Evaluate(AnimationGraphContext context)
        {
            return locomotionPose.Evaluate(context);
        }

        public override AnimationNodeDebugSnapshot GetDebugSnapshot()
        {
            return new AnimationNodeDebugSnapshot(nameof(WeaponAnimationLayer), IsInitialized && !IsDisposed, IsPoseAvailable ? 1f : 0f, 1);
        }

        protected override void OnInitialize(AnimationGraphContext context)
        {
            IsDisposed = false;
            locomotionPose.Initialize(context);
        }

        protected override void OnDestroy()
        {
            locomotionPose.Destroy();
            IsDisposed = true;
        }
    }
}
