using System;
using System.Collections.Generic;

namespace CGame
{
    public readonly struct CameraCompositionResult
    {
        private readonly IReadOnlyList<CameraLayerContribution> contributions;

        public CameraCompositionResult(
            CameraPose basePose,
            CameraLensState baseLens,
            CameraPose finalPose,
            CameraLensState finalLens,
            CameraLayerContribution[] contributions)
        {
            BasePose = basePose;
            BaseLens = baseLens;
            FinalPose = finalPose;
            FinalLens = finalLens;
            CameraLayerContribution[] copy = contributions == null
                ? Array.Empty<CameraLayerContribution>()
                : (CameraLayerContribution[])contributions.Clone();
            this.contributions = Array.AsReadOnly(copy);
        }

        public CameraPose BasePose { get; }
        public CameraLensState BaseLens { get; }
        public CameraPose FinalPose { get; }
        public CameraLensState FinalLens { get; }
        public IReadOnlyList<CameraLayerContribution> Contributions => contributions ?? Array.Empty<CameraLayerContribution>();
    }
}
