using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGame
{
    public sealed class CameraLocomotionEffects
    {
        private readonly CameraLocomotionEffectProfile profile;
        private float movementWeight;
        private float stanceOffset;
        private float bobPhase;
        private float breathPhase;

        public CameraLocomotionEffects(CameraLocomotionEffectProfile profile)
        {
            this.profile = profile != null ? profile : throw new ArgumentNullException(nameof(profile));
        }

        public IReadOnlyList<CameraLayerContribution> Evaluate(CameraLocomotionSample sample, float deltaTime)
        {
            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            float speedWeight = sample.IsGrounded
                ? Mathf.InverseLerp(0.05f, profile.SprintSpeed, sample.HorizontalSpeed)
                : 0f;
            movementWeight = Damp(movementWeight, speedWeight, profile.MovementResponse, safeDeltaTime);
            float targetStanceOffset = sample.IsGrounded ? 0f : profile.AirborneEyeOffset;
            stanceOffset = Damp(stanceOffset, targetStanceOffset, profile.StanceResponse, safeDeltaTime);

            if (safeDeltaTime > 0f)
            {
                float gait = Mathf.InverseLerp(profile.WalkSpeed, profile.SprintSpeed, sample.HorizontalSpeed);
                float bobFrequency = Mathf.Lerp(profile.WalkBobFrequency, profile.SprintBobFrequency, gait);
                if (sample.IsGrounded && sample.HorizontalSpeed > 0.05f)
                {
                    bobPhase = Mathf.Repeat(bobPhase + Mathf.PI * 2f * bobFrequency * safeDeltaTime, Mathf.PI * 2f);
                }

                breathPhase = Mathf.Repeat(
                    breathPhase + Mathf.PI * 2f * profile.BreathFrequency * safeDeltaTime,
                    Mathf.PI * 2f);
            }

            CameraPoseDelta stance = BuildStanceDelta();
            CameraPoseDelta bob = BuildBobDelta(sample.HorizontalSpeed);
            CameraPoseDelta swayAndBreath = BuildSwayAndBreathDelta(sample.IsGrounded);
            return Array.AsReadOnly(new[]
            {
                new CameraLayerContribution(CameraEffectLayer.Stance, stance, CameraLensDelta.None, stance.Weight > 0f),
                new CameraLayerContribution(CameraEffectLayer.Bob, bob, CameraLensDelta.None, bob.Weight > 0f),
                new CameraLayerContribution(CameraEffectLayer.Sway, swayAndBreath, CameraLensDelta.None, swayAndBreath.Weight > 0f)
            });
        }

        public void Reset()
        {
            movementWeight = 0f;
            stanceOffset = 0f;
            bobPhase = 0f;
            breathPhase = 0f;
        }

        private CameraPoseDelta BuildStanceDelta()
        {
            float weight = Mathf.Abs(stanceOffset) > 0.000001f ? profile.StanceWeight : 0f;
            return new CameraPoseDelta(
                ClampPosition(new Vector3(0f, stanceOffset, 0f)),
                Quaternion.identity,
                weight);
        }

        private CameraPoseDelta BuildBobDelta(float horizontalSpeed)
        {
            float gait = Mathf.InverseLerp(profile.WalkSpeed, profile.SprintSpeed, horizontalSpeed);
            Vector3 amplitude = Vector3.Lerp(profile.WalkBobPosition, profile.SprintBobPosition, gait);
            var position = new Vector3(
                Mathf.Sin(bobPhase) * amplitude.x,
                -Mathf.Cos(bobPhase * 2f) * amplitude.y,
                amplitude.z);
            float weight = profile.BobWeight * movementWeight;
            return new CameraPoseDelta(ClampPosition(position), Quaternion.identity, weight);
        }

        private CameraPoseDelta BuildSwayAndBreathDelta(bool isGrounded)
        {
            float movementSine = Mathf.Sin(bobPhase * 0.5f);
            float swayScale = profile.SwayWeight * movementWeight;
            float breathScale = profile.BreathWeight * (isGrounded ? 1f - movementWeight : 0f);
            float breathSine = Mathf.Sin(breathPhase);
            Vector3 position = profile.SwayPositionAmplitude * (movementSine * swayScale);
            position.y += profile.BreathPositionAmplitude * breathSine * breathScale;
            Vector3 rotation = profile.SwayRotationAmplitude * (movementSine * swayScale);
            rotation.x += profile.BreathPitchAmplitude * breathSine * breathScale;
            position = ClampPosition(position);
            rotation.x = Mathf.Clamp(rotation.x, -profile.MaxLocalRotation, profile.MaxLocalRotation);
            rotation.y = Mathf.Clamp(rotation.y, -profile.MaxLocalRotation, profile.MaxLocalRotation);
            rotation.z = Mathf.Clamp(rotation.z, -profile.MaxLocalRotation, profile.MaxLocalRotation);
            bool hasContribution = position.sqrMagnitude > 0.0000000001f || rotation.sqrMagnitude > 0.0000000001f;
            return new CameraPoseDelta(position, Quaternion.Euler(rotation), hasContribution ? 1f : 0f);
        }

        private Vector3 ClampPosition(Vector3 position)
        {
            return Vector3.ClampMagnitude(position, profile.MaxLocalPosition);
        }

        private static float Damp(float current, float target, float response, float deltaTime)
        {
            if (deltaTime <= 0f || Mathf.Approximately(current, target))
            {
                return current;
            }

            float alpha = 1f - Mathf.Exp(-Mathf.Max(0f, response) * deltaTime);
            return Mathf.Lerp(current, target, alpha);
        }
    }
}
