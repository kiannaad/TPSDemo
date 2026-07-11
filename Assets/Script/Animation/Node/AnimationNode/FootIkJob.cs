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

        public void ProcessAnimation(AnimationStream stream)
        {
            if (!stream.isHumanStream || LeftWeight <= 0f && RightWeight <= 0f)
            {
                return;
            }

            AnimationHumanStream human = stream.AsHuman();
            Vector3 leftPosePosition = human.GetGoalPositionFromPose(AvatarIKGoal.LeftFoot);
            Vector3 rightPosePosition = human.GetGoalPositionFromPose(AvatarIKGoal.RightFoot);
            float leftContact = CalculateContactWeight(leftPosePosition, LeftPosition, LeftNormal, FullContactHeight, ReleaseHeight);
            float rightContact = CalculateContactWeight(rightPosePosition, RightPosition, RightNormal, FullContactHeight, ReleaseHeight);
            human.SetGoalPosition(AvatarIKGoal.LeftFoot, LeftPosition);
            human.SetGoalRotation(AvatarIKGoal.LeftFoot, LeftRotation);
            human.SetGoalPosition(AvatarIKGoal.RightFoot, RightPosition);
            human.SetGoalRotation(AvatarIKGoal.RightFoot, RightRotation);
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

        public void ProcessRootMotion(AnimationStream stream)
        {
        }
    }
}
