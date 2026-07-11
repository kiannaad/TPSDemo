using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame.Animation
{
    public sealed class FootIkNode : AnimationNodeBase
    {
        private const int HitCapacity = 8;
        private readonly IAnimationPlayableNode inputNode;
        private readonly Animator animator;
        private readonly Transform leftFoot;
        private readonly Transform rightFoot;
        private readonly RaycastHit[] hits = new RaycastHit[HitCapacity];
        private AnimationScriptPlayable scriptPlayable;
        private Vector3 leftTargetPosition;
        private Quaternion leftTargetRotation;
        private Vector3 leftGroundNormal = Vector3.up;
        private Vector3 rightTargetPosition;
        private Quaternion rightTargetRotation;
        private Vector3 rightGroundNormal = Vector3.up;
        private float leftWeight;
        private float rightWeight;

        public FootIkNode(IAnimationPlayableNode inputNode, Animator animator, Transform leftFoot, Transform rightFoot)
        {
            this.inputNode = inputNode ?? throw new ArgumentNullException(nameof(inputNode));
            this.animator = animator ?? throw new ArgumentNullException(nameof(animator));
            this.leftFoot = leftFoot ?? throw new ArgumentNullException(nameof(leftFoot));
            this.rightFoot = rightFoot ?? throw new ArgumentNullException(nameof(rightFoot));
        }

        public float ProbeHeight { get; set; } = 0.35f;
        public float ProbeDistance { get; set; } = 0.7f;
        public float FootHeight { get; set; } = 0.08f;
        public float MaxCorrection { get; set; } = 0.25f;
        public float WeightSharpness { get; set; } = 16f;
        public float FullContactHeight { get; set; } = 0.03f;
        public float ReleaseHeight { get; set; } = 0.12f;
        public LayerMask GroundLayers { get; set; } = ~0;
        public float LeftWeight => leftWeight;
        public float RightWeight => rightWeight;
        public AnimationScriptPlayable ScriptPlayable => scriptPlayable;

        public override void Update(AnimationGraphContext context, float deltaTime)
        {
            inputNode.Update(context, deltaTime);
            float targetWeight = context.IsGrounded ? 1f : 0f;
            UpdateFoot(leftFoot, targetWeight, deltaTime, ref leftTargetPosition, ref leftTargetRotation, ref leftGroundNormal, ref leftWeight);
            UpdateFoot(rightFoot, targetWeight, deltaTime, ref rightTargetPosition, ref rightTargetRotation, ref rightGroundNormal, ref rightWeight);

            FootIkJob job = scriptPlayable.GetJobData<FootIkJob>();
            job.LeftPosition = leftTargetPosition;
            job.LeftRotation = leftTargetRotation;
            job.LeftNormal = leftGroundNormal;
            job.LeftWeight = leftWeight;
            job.RightPosition = rightTargetPosition;
            job.RightRotation = rightTargetRotation;
            job.RightNormal = rightGroundNormal;
            job.RightWeight = rightWeight;
            job.FullContactHeight = Mathf.Max(0f, FullContactHeight);
            job.ReleaseHeight = Mathf.Max(job.FullContactHeight, ReleaseHeight);
            scriptPlayable.SetJobData(job);
        }

        public override AnimationPoseHandle Evaluate(AnimationGraphContext context)
        {
            inputNode.Evaluate(context);
            return new AnimationPoseHandle(scriptPlayable, 1f, context.EvaluateFrameId, nameof(FootIkNode));
        }

        public override AnimationNodeDebugSnapshot GetDebugSnapshot()
        {
            return new AnimationNodeDebugSnapshot(nameof(FootIkNode), scriptPlayable.IsValid(), Mathf.Max(leftWeight, rightWeight), 1);
        }

        protected override void OnInitialize(AnimationGraphContext context)
        {
            inputNode.Initialize(context);
            leftTargetPosition = leftFoot.position;
            leftTargetRotation = leftFoot.rotation;
            rightTargetPosition = rightFoot.position;
            rightTargetRotation = rightFoot.rotation;
            var job = new FootIkJob
            {
                LeftPosition = leftTargetPosition,
                LeftRotation = leftTargetRotation,
                LeftNormal = Vector3.up,
                RightPosition = rightTargetPosition,
                RightRotation = rightTargetRotation,
                RightNormal = Vector3.up,
                FullContactHeight = FullContactHeight,
                ReleaseHeight = ReleaseHeight,
            };
            scriptPlayable = AnimationScriptPlayable.Create(context.Graph, job, 1);
            context.Graph.Connect(inputNode.Evaluate(context).Playable, 0, scriptPlayable, 0);
            scriptPlayable.SetInputWeight(0, 1f);
        }

        protected override void OnDestroy()
        {
            inputNode.Destroy();
        }

        private void UpdateFoot(
            Transform foot,
            float targetWeight,
            float deltaTime,
            ref Vector3 targetPosition,
            ref Quaternion targetRotation,
            ref Vector3 groundNormal,
            ref float weight)
        {
            RaycastHit hit = default;
            bool hasGround = targetWeight > 0f && TryGetGround(foot.position, out hit);
            float weightAlpha = GetSharpnessAlpha(WeightSharpness, deltaTime);
            if (hasGround)
            {
                Vector3 desiredPosition = hit.point + hit.normal * FootHeight;
                float penetration = Vector3.Dot(desiredPosition - foot.position, hit.normal);
                Vector3 correction = hit.normal * Mathf.Clamp(penetration, 0f, MaxCorrection);
                targetPosition = foot.position + correction;
                groundNormal = hit.normal;
                Vector3 forward = Vector3.ProjectOnPlane(foot.forward, hit.normal);
                if (forward.sqrMagnitude <= 0.0001f)
                {
                    forward = Vector3.ProjectOnPlane(animator.transform.forward, hit.normal);
                }

                targetRotation = Quaternion.LookRotation(forward.normalized, hit.normal);
            }
            else
            {
                targetPosition = foot.position;
                targetRotation = foot.rotation;
                groundNormal = Vector3.up;
            }

            weight = Mathf.Lerp(weight, hasGround ? targetWeight : 0f, weightAlpha);
        }

        private bool TryGetGround(Vector3 footPosition, out RaycastHit closestHit)
        {
            Vector3 origin = footPosition + Vector3.up * ProbeHeight;
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                hits,
                ProbeHeight + ProbeDistance,
                GroundLayers,
                QueryTriggerInteraction.Ignore);
            float closestDistance = float.MaxValue;
            closestHit = default;
            for (int i = 0; i < hitCount; i++)
            {
                Transform hitTransform = hits[i].collider.transform;
                if (hitTransform.IsChildOf(animator.transform) || animator.transform.IsChildOf(hitTransform))
                {
                    continue;
                }

                if (hits[i].distance < closestDistance)
                {
                    closestDistance = hits[i].distance;
                    closestHit = hits[i];
                }
            }

            return closestDistance < float.MaxValue;
        }

        private static float GetSharpnessAlpha(float sharpness, float deltaTime)
        {
            return deltaTime <= 0f ? 1f : 1f - Mathf.Exp(-Mathf.Max(0f, sharpness) * deltaTime);
        }
    }
}
