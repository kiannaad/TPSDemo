using UnityEngine;
using UnityEngine.Animations;

namespace CGame.Animation
{
    public struct FootIkJob : IAnimationJob
    {
        public Vector3 LeftPosition;
        public Quaternion LeftRotation;
        public Vector3 LeftNormal;
        public float LeftWeight;
        public Vector3 RightPosition;
        public Quaternion RightRotation;
        public Vector3 RightNormal;
        public float RightWeight;
        public float FullContactHeight;
        public float ReleaseHeight;
        public float MaxCorrection;

        public void ProcessAnimation(AnimationStream stream)
        {
            if (!stream.isHumanStream || LeftWeight <= 0f && RightWeight <= 0f)
            {
                return;
            }

            AnimationHumanStream human = stream.AsHuman();
            Vector3 leftPosePosition = human.GetGoalPositionFromPose(AvatarIKGoal.LeftFoot);
            Vector3 rightPosePosition = human.GetGoalPositionFromPose(AvatarIKGoal.RightFoot);
            Quaternion leftPoseRotation = human.GetGoalRotationFromPose(AvatarIKGoal.LeftFoot);
            Quaternion rightPoseRotation = human.GetGoalRotationFromPose(AvatarIKGoal.RightFoot);
            float leftContact = CalculateContactWeight(leftPosePosition, LeftPosition, LeftNormal, FullContactHeight, ReleaseHeight);
            float rightContact = CalculateContactWeight(rightPosePosition, RightPosition, RightNormal, FullContactHeight, ReleaseHeight);
            human.SetGoalPosition(AvatarIKGoal.LeftFoot, CalculateGoalPosition(leftPosePosition, LeftPosition, LeftNormal, MaxCorrection));
            human.SetGoalRotation(AvatarIKGoal.LeftFoot, CalculateGoalRotation(leftPoseRotation, LeftNormal));
            human.SetGoalPosition(AvatarIKGoal.RightFoot, CalculateGoalPosition(rightPosePosition, RightPosition, RightNormal, MaxCorrection));
            human.SetGoalRotation(AvatarIKGoal.RightFoot, CalculateGoalRotation(rightPoseRotation, RightNormal));
            human.SetGoalWeightPosition(AvatarIKGoal.LeftFoot, Mathf.Clamp01(LeftWeight * leftContact));
            human.SetGoalWeightRotation(AvatarIKGoal.LeftFoot, Mathf.Clamp01(LeftWeight * leftContact));
            human.SetGoalWeightPosition(AvatarIKGoal.RightFoot, Mathf.Clamp01(RightWeight * rightContact));
            human.SetGoalWeightRotation(AvatarIKGoal.RightFoot, Mathf.Clamp01(RightWeight * rightContact));
            human.SolveIK();
        }

        public static float CalculateContactWeight(
            Vector3 posePosition,
            Vector3 targetPosition,
            Vector3 groundNormal,
            float fullContactHeight,
            float releaseHeight)
        {
            Vector3 normal = groundNormal.sqrMagnitude > 0.0001f ? groundNormal.normalized : Vector3.up;
            float clearance = Vector3.Dot(posePosition - targetPosition, normal);
            if (releaseHeight <= fullContactHeight)
            {
                return clearance <= fullContactHeight ? 1f : 0f;
            }

            return 1f - Mathf.InverseLerp(fullContactHeight, releaseHeight, clearance);
        }

        public static Vector3 CalculateGoalPosition(
            Vector3 posePosition,
            Vector3 groundPosition,
            Vector3 groundNormal,
            float maxCorrection)
        {
            Vector3 normal = groundNormal.sqrMagnitude > 0.0001f ? groundNormal.normalized : Vector3.up;
            float penetration = Vector3.Dot(groundPosition - posePosition, normal);
            return posePosition + normal * Mathf.Clamp(penetration, 0f, Mathf.Max(0f, maxCorrection));
        }

        private static Quaternion CalculateGoalRotation(Quaternion poseRotation, Vector3 groundNormal)
        {
            Vector3 normal = groundNormal.sqrMagnitude > 0.0001f ? groundNormal.normalized : Vector3.up;
            Vector3 forward = Vector3.ProjectOnPlane(poseRotation * Vector3.forward, normal);
            return forward.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(forward.normalized, normal)
                : poseRotation;
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }
    }
}
