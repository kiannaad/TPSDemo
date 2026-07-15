using UnityEngine;
using UnityEngine.Animations;

namespace CGame.Animation
{
    public struct HumanoidLeftHandIkJob : IAnimationJob
    {
        public Vector3 TargetPosition;
        public Quaternion TargetRotation;
        public float Weight;

        public void ProcessAnimation(AnimationStream stream)
        {
            if (!stream.isHumanStream || Weight <= 0f)
            {
                return;
            }

            AnimationHumanStream human = stream.AsHuman();
            Vector3 bodyPosition = human.bodyPosition;
            Quaternion bodyRotation = human.bodyRotation;
            human.SetGoalPosition(AvatarIKGoal.LeftHand, TargetPosition);
            human.SetGoalRotation(AvatarIKGoal.LeftHand, TargetRotation);
            human.SetGoalWeightPosition(AvatarIKGoal.LeftHand, Mathf.Clamp01(Weight));
            // Keep a small amount of the authored wrist pose while aligning the palm with
            // the calibrated underside support target. Full rotation still over-constrains
            // the retargeted wrist during locomotion.
            human.SetGoalWeightRotation(AvatarIKGoal.LeftHand, Mathf.Clamp01(Weight) * 0.8f);
            human.SolveIK();
            human.bodyPosition = bodyPosition;
            human.bodyRotation = bodyRotation;
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }
    }
}
