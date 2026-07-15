using System;
using UnityEngine;

namespace CGame
{
    public static class AICombatAimModel
    {
        public static float CalculateErrorDegrees(
            CombatProfile profile,
            float convergence,
            bool isMoving,
            float pressure)
        {
            if (profile == null || !profile.IsValid)
            {
                throw new ArgumentException("A valid combat profile is required.", nameof(profile));
            }

            float clampedConvergence = Mathf.Clamp01(convergence);
            float error = profile.BaseAimErrorDegrees
                + (1f - clampedConvergence) * profile.UnconvergedAimErrorDegrees;
            if (isMoving)
            {
                error += profile.MovementAimErrorDegrees;
            }

            error += Mathf.Clamp01(pressure) * profile.PressureAimErrorDegrees;
            return error;
        }
    }
}
