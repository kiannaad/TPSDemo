using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGame.Animation
{
    public sealed class CharacterAnimationGraph : IDisposable
    {
        private static readonly string[] compositionOrder =
        {
            "FullBodyAction",
            "UpperBodyWeaponAction",
            "AimAdditive",
            "AdditiveReaction",
            "LeftHandIK",
            "RootDelta",
        };
        private readonly OutputNode output;
        private readonly AnimationStateMachineNode locomotionStateMachine;
        private readonly InertializationNode inertializationNode;
        private readonly WeaponAnimationDefinitionResolver weaponDefinitionResolver;
        private readonly WeaponLayerBlendNode weaponLayerBlendNode;
        private readonly ActionNode fireActionNode;
        private readonly AimOffsetNode aimOffsetNode;
        private readonly RecoilReactionNode recoilReactionNode;
        private readonly HumanoidLeftHandIkNode leftHandIkNode;
        private readonly Transform observerSpine;
        private readonly Transform observerChest;
        private WeaponAnimationDefinition appliedWeaponDefinition;
        private WeaponEquipmentSnapshot appliedWeaponSnapshot;

        public event Action<ActionPresentationEnded> PresentationEnded;

        public CharacterAnimationGraph(Animator animator, CharacterAnimationConfig config)
        {
            if (animator == null) throw new ArgumentNullException(nameof(animator));
            if (config == null || !config.IsValid) throw new ArgumentException("A valid character animation config is required.", nameof(config));

            var move = new Blend1DNode(new[]
            {
                new Blend1DChild(new ClipNode(config.Walk.AnimationClip), 1f),
                new Blend1DChild(new ClipNode(config.Run.AnimationClip), 3f),
            }, context => context.MoveSpeed);
            AnimationState[] states =
            {
                new AnimationState("Idle", new ClipNode(config.Idle.AnimationClip)),
                new AnimationState("Move", move),
                new AnimationState("Stop", CreateOneShot(config.Stop.AnimationClip)),
                new AnimationState("Air", new ClipNode(config.InAir.AnimationClip)),
                new AnimationState("Land", CreateOneShot(config.Land.AnimationClip)),
            };
            AnimationStateTransition[] transitions =
            {
                new AnimationStateTransition("Idle", "Move", context => context.IsGrounded && context.MoveSpeed > 0.1f),
                new AnimationStateTransition("Move", "Stop", context => context.IsGrounded && context.MoveSpeed <= 0.1f),
                new AnimationStateTransition("Stop", "Move", context => context.IsGrounded && context.MoveSpeed > 0.1f, 10),
                new AnimationStateTransition("Stop", "Idle", context => context.IsGrounded && context.MoveSpeed <= 0.1f),
                new AnimationStateTransition(string.Empty, "Air", context => !context.IsGrounded, 100, 0.08f),
                new AnimationStateTransition("Air", "Land", context => context.IsGrounded, 120, 0.08f),
                new AnimationStateTransition("Land", "Move", context => context.IsGrounded && context.MoveSpeed > 0.1f, 10, 0.1f),
                new AnimationStateTransition("Land", "Idle", context => context.IsGrounded && context.MoveSpeed <= 0.1f, 0, 0.1f),
            };
            locomotionStateMachine = new AnimationStateMachineNode(states, transitions, "Idle");
            var cachedLocomotion = new CachedPoseNode(locomotionStateMachine);
            weaponDefinitionResolver = new WeaponAnimationDefinitionResolver(config.WeaponDefinitions);
            AvatarMask upperBodyMask = CreateUpperBodyMask();
            weaponLayerBlendNode = new WeaponLayerBlendNode(cachedLocomotion, upperBodyMask);
            WeaponAnimationDefinition initialWeaponDefinition = FindInitialWeaponDefinition(config);
            if (initialWeaponDefinition.Fire == null || !initialWeaponDefinition.Fire.IsValid)
            {
                throw new ArgumentException("The initial weapon definition requires a valid Fire action asset.", nameof(config));
            }

            fireActionNode = new ActionNode(initialWeaponDefinition.Fire, 10);
            fireActionNode.PresentationEnded += OnActionPresentationEnded;
            var upperBodyWeaponAction = new PriorityNode(fireActionNode);
            var layered = new LayeredBlendPerBoneNode(
                weaponLayerBlendNode,
                // The imported AK package has a real weapon-mechanism fire clip but no compatible
                // character fire pose. Keep this channel authoritative for action lifetime/notifies
                // without overriding locomotion; the visible impulse belongs to AdditiveReaction.
                new LayeredAnimationInput(upperBodyWeaponAction, upperBodyMask, context => 0f, false, "UpperBodyWeaponAction"));

            aimOffsetNode = new AimOffsetNode(layered, animator);
            recoilReactionNode = new RecoilReactionNode(aimOffsetNode, animator);
            IAnimationPlayableNode root = recoilReactionNode;
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips != null)
            {
                inertializationNode = new InertializationNode(root, hips);
                root = inertializationNode;
                locomotionStateMachine.StatePhaseChanged += OnStatePhaseChanged;
            }

            Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            Transform chest = animator.GetBoneTransform(HumanBodyBones.UpperChest)
                ?? animator.GetBoneTransform(HumanBodyBones.Chest);
            observerSpine = chest != null
                ? spine
                : animator.GetBoneTransform(HumanBodyBones.Neck) ?? spine;
            observerChest = chest ?? animator.GetBoneTransform(HumanBodyBones.Head);

            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (animator.isHuman && leftHand != null)
            {
                leftHandIkNode = new HumanoidLeftHandIkNode(root);
                root = leftHandIkNode;
            }

            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (leftFoot != null && rightFoot != null)
            {
                root = new FootIkNode(root, animator, leftFoot, rightFoot);
            }

            root = new RootDeltaNode(root);
            output = new OutputNode(root, "CharacterAnimationGraph");
            output.Initialize(animator);
        }

        public AnimationGraphContext Context => output.Context;
        public string CurrentLocomotionState => locomotionStateMachine.CurrentState;
        public bool IsInitialized => output.IsInitialized;
        public WeaponId EquippedWeaponId => appliedWeaponSnapshot.EquippedWeaponId;
        public uint WeaponGeneration => appliedWeaponSnapshot.Generation;
        public WeaponLayerBlendNode WeaponLayerBlend => weaponLayerBlendNode;
        public AimOffsetNode AimOffset => aimOffsetNode;
        public ActionNode FireAction => fireActionNode;
        public RecoilReactionNode RecoilReaction => recoilReactionNode;
        public HumanoidLeftHandIkNode LeftHandIk => leftHandIkNode;
        public static IReadOnlyList<string> CompositionOrder => compositionOrder;

        public void ApplyWeaponEquipment(
            WeaponEquipmentSnapshot snapshot,
            WeaponPresentationBinding binding = null,
            bool force = false)
        {
            if (!force
                && snapshot.Generation == appliedWeaponSnapshot.Generation
                && snapshot.EquippedWeaponId == appliedWeaponSnapshot.EquippedWeaponId)
            {
                return;
            }

            appliedWeaponSnapshot = snapshot;
            Context.ActiveWeaponGeneration = snapshot.Generation;
            EndActivePresentation(ActionPresentationEndReason.EquipmentChanged);
            recoilReactionNode.Cancel();
            if (!snapshot.IsEquipped)
            {
                appliedWeaponDefinition = null;
                Context.AimWeight = 0f;
                Context.LeftHandIkWeight = 0f;
                leftHandIkNode?.SetBinding(null, 0.08f);
                weaponLayerBlendNode.SetTarget(null, 0.15f);
                return;
            }

            if (!weaponDefinitionResolver.TryResolve(snapshot.EquippedWeaponId, out WeaponAnimationDefinition definition))
            {
                appliedWeaponDefinition = null;
                Context.AimWeight = 0f;
                Context.LeftHandIkWeight = 0f;
                leftHandIkNode?.SetBinding(null, 0.08f);
                weaponLayerBlendNode.SetTarget(null, 0.15f);
                Context.RecordDebugEvent(nameof(CharacterAnimationGraph), "WeaponDefinitionFallback", snapshot.Generation);
                return;
            }

            appliedWeaponDefinition = definition;
            var nextLayer = new WeaponAnimationLayer(definition, snapshot.Generation, () => locomotionStateMachine.CurrentState);
            weaponLayerBlendNode.SetTarget(nextLayer, definition.BlendDuration);
            aimOffsetNode.Configure(
                definition.AimYawRange,
                definition.AimPitchUpRange,
                definition.AimPitchDownRange,
                definition.AimWeight,
                definition.AimSmoothingTime);
            Context.AimWeight = 1f;
            bool hasGrip = binding != null && binding.CanConsume(snapshot.Generation);
            Context.LeftHandIkWeight = hasGrip ? 1f : 0f;
            leftHandIkNode?.SetBinding(binding, definition.LeftHandIkSmoothingTime);
            recoilReactionNode.Configure(definition.RecoilImpulse, definition.RecoilMaxPitch, definition.RecoilDecayTime);
            if (!hasGrip)
            {
                Context.RecordDebugEvent(nameof(CharacterAnimationGraph), "LeftHandIkDegraded", snapshot.Generation);
            }
        }

        public void BeginWeaponEquipmentTransition(WeaponEquipmentSnapshot snapshot)
        {
            appliedWeaponSnapshot = snapshot;
            Context.ActiveWeaponGeneration = snapshot.Generation;
            EndActivePresentation(ActionPresentationEndReason.EquipmentChanged);
            recoilReactionNode.Cancel();
            appliedWeaponDefinition = null;
            Context.AimWeight = 0f;
            Context.LeftHandIkWeight = 0f;
            leftHandIkNode?.SetBinding(null, 0.08f);
        }

        public void ApplyWeaponFallback(WeaponEquipmentSnapshot snapshot, string missingField)
        {
            BeginWeaponEquipmentTransition(snapshot);
            weaponLayerBlendNode.SetTarget(null, 0.15f);
            Context.RecordDebugEvent(
                nameof(CharacterAnimationGraph),
                $"WeaponPresentationFallback:{missingField}",
                snapshot.Generation);
        }

        public void SetAimInput(float yaw, float pitch)
        {
            Context.AimYaw = yaw;
            Context.AimPitch = pitch;
        }

        public bool StartWeaponAction(WeaponActionFact fact)
        {
            if (!CanConsume(fact) || fact.Phase != WeaponActionPhase.Started || fact.Kind != WeaponActionKind.Fire)
            {
                return false;
            }

            fireActionNode.Request(fact.ActionId);
            Context.RecordDebugEvent(nameof(CharacterAnimationGraph), $"UpperBodyWeaponAction:Started:{fact.ActionId}");
            return true;
        }

        public bool CommitFireReaction(WeaponActionFact fact)
        {
            if (!CanConsume(fact) || fact.Kind != WeaponActionKind.Fire)
            {
                return false;
            }

            bool triggered = recoilReactionNode.Trigger(fact.ActionId);
            if (triggered)
            {
                Context.RecordDebugEvent(nameof(CharacterAnimationGraph), $"AdditiveReaction:FireCommitted:{fact.ActionId}");
            }
            return triggered;
        }

        public bool EndWeaponAction(WeaponActionFact fact)
        {
            if (fact.ActionId == 0ul)
            {
                return false;
            }

            ActionPresentationEndReason reason = MapEndReason(fact);
            bool ended = fireActionNode.End(fact.ActionId, reason);
            if (fact.Phase == WeaponActionPhase.Cancelled
                && fact.EndReason != WeaponActionEndReason.Superseded)
            {
                recoilReactionNode.Cancel();
            }
            return ended;
        }

        public void Update(float deltaTime)
        {
            output.Update(deltaTime);
            ApplyObserverAim();
        }

        public AnimationGraphDebugSnapshot GetDebugSnapshot() => output.GetGraphDebugSnapshot();

        public void Dispose()
        {
            locomotionStateMachine.StatePhaseChanged -= OnStatePhaseChanged;
            fireActionNode.PresentationEnded -= OnActionPresentationEnded;
            output.Destroy();
        }

        private void OnStatePhaseChanged(string state, LocomotionStatePhase phase)
        {
            if (phase == LocomotionStatePhase.Enter)
            {
                inertializationNode?.Request(0.12f);
            }
        }

        private void ApplyObserverAim()
        {
            AnimationGraphContext context = Context;
            if (observerSpine == null || context.ObserverAimWeight <= 0f)
            {
                return;
            }

            bool hasChest = observerChest != null && observerChest != observerSpine;
            float spineShare = hasChest ? 0.4f : 0.65f;
            ApplyObserverAimRotation(observerSpine, context, spineShare);
            if (hasChest)
            {
                ApplyObserverAimRotation(observerChest, context, 0.6f);
            }
        }

        private static void ApplyObserverAimRotation(
            Transform bone,
            AnimationGraphContext context,
            float share)
        {
            Quaternion poseRotation = bone.localRotation;
            Quaternion aimOffset = Quaternion.Euler(
                -context.ObserverAimPitch * share,
                context.ObserverAimYawOffset * share,
                0f);
            Quaternion aimedRotation = poseRotation * aimOffset;
            bone.localRotation = Quaternion.Slerp(
                poseRotation,
                aimedRotation,
                Mathf.Clamp01(context.ObserverAimWeight));
        }

        private static ClipNode CreateOneShot(AnimationClip clip)
        {
            return new ClipNode(clip) { Loop = false };
        }

        private bool CanConsume(WeaponActionFact fact)
        {
            return fact.IsValid
                && appliedWeaponDefinition != null
                && fact.Generation == appliedWeaponSnapshot.Generation
                && fact.WeaponId == appliedWeaponSnapshot.EquippedWeaponId;
        }

        private void EndActivePresentation(ActionPresentationEndReason reason)
        {
            if (fireActionNode.IsActive)
            {
                fireActionNode.End(fireActionNode.RequestId, reason);
            }
            else if (fireActionNode.PendingRequestId > 0ul)
            {
                fireActionNode.End(fireActionNode.PendingRequestId, reason);
            }
        }

        private void OnActionPresentationEnded(ActionPresentationEnded ended)
        {
            Context.RecordDebugEvent(nameof(CharacterAnimationGraph), $"PresentationEnded:{ended.RequestId}:{ended.Reason}");
            PresentationEnded?.Invoke(ended);
        }

        private static ActionPresentationEndReason MapEndReason(WeaponActionFact fact)
        {
            if (fact.Phase == WeaponActionPhase.Completed)
            {
                return ActionPresentationEndReason.GameplayCompleted;
            }

            switch (fact.EndReason)
            {
                case WeaponActionEndReason.EquipmentChanged:
                case WeaponActionEndReason.Unequipped:
                    return ActionPresentationEndReason.EquipmentChanged;
                case WeaponActionEndReason.OwnerDisposed:
                    return ActionPresentationEndReason.OwnerDisposed;
                default:
                    return ActionPresentationEndReason.GameplayCancelled;
            }
        }

        private static WeaponAnimationDefinition FindInitialWeaponDefinition(CharacterAnimationConfig config)
        {
            WeaponAnimationDefinition[] definitions = config.WeaponDefinitions;
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] != null && definitions[i].IsValid)
                {
                    return definitions[i];
                }
            }

            throw new ArgumentException("A valid weapon animation definition is required.", nameof(config));
        }

        private static AvatarMask CreateUpperBodyMask()
        {
            var mask = new AvatarMask();
            for (AvatarMaskBodyPart part = AvatarMaskBodyPart.Root; part < AvatarMaskBodyPart.LastBodyPart; part++)
            {
                mask.SetHumanoidBodyPartActive(part, false);
            }

            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            return mask;
        }
    }
}
