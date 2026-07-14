using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGame
{
    public static class CameraPoseCompositor
    {
        private static readonly CameraEffectLayer[] LayerOrder =
        {
            CameraEffectLayer.Stance,
            CameraEffectLayer.Bob,
            CameraEffectLayer.Sway,
            CameraEffectLayer.VisualRecoil,
            CameraEffectLayer.Impulse,
            CameraEffectLayer.Lens
        };

        public static CameraCompositionResult Compose(
            CameraPose basePose,
            CameraLensState baseLens,
            IReadOnlyList<CameraLayerContribution> contributions)
        {
            var orderedContributions = new CameraLayerContribution[LayerOrder.Length];
            var assignedLayers = new bool[LayerOrder.Length];
            for (int index = 0; index < LayerOrder.Length; index++)
            {
                orderedContributions[index] = CameraLayerContribution.Disabled(LayerOrder[index]);
            }

            if (contributions != null)
            {
                for (int index = 0; index < contributions.Count; index++)
                {
                    CameraLayerContribution contribution = contributions[index];
                    int layerIndex = Array.IndexOf(LayerOrder, contribution.Layer);
                    if (layerIndex < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(contributions), contribution.Layer, "Unknown Camera effect layer.");
                    }

                    if (assignedLayers[layerIndex])
                    {
                        throw new ArgumentException($"Only one contribution is allowed for {contribution.Layer}.", nameof(contributions));
                    }

                    assignedLayers[layerIndex] = true;
                    orderedContributions[layerIndex] = contribution;
                }
            }

            CameraPose finalPose = basePose;
            CameraLensState finalLens = baseLens;
            for (int index = 0; index < orderedContributions.Length; index++)
            {
                CameraLayerContribution contribution = orderedContributions[index];
                if (!contribution.IsEnabled)
                {
                    continue;
                }

                CameraPoseDelta poseDelta = contribution.PoseDelta;
                if (poseDelta.Weight > 0f)
                {
                    Vector3 localOffset = poseDelta.LocalPosition * poseDelta.Weight;
                    Quaternion localRotation = Quaternion.Slerp(Quaternion.identity, poseDelta.LocalRotation, poseDelta.Weight);
                    finalPose = new CameraPose(
                        finalPose.Position + finalPose.Rotation * localOffset,
                        finalPose.Rotation * localRotation);
                }

                CameraLensDelta lensDelta = contribution.LensDelta;
                if (lensDelta.Weight > 0f)
                {
                    finalLens = new CameraLensState(finalLens.FieldOfView + lensDelta.FieldOfView * lensDelta.Weight);
                }
            }

            return new CameraCompositionResult(basePose, baseLens, finalPose, finalLens, orderedContributions);
        }
    }
}
