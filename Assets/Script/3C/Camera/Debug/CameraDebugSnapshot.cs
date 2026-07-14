using System.Collections.Generic;
using UnityEngine;

namespace CGame
{
    public readonly struct CameraDebugSnapshot
    {
        public CameraDebugSnapshot(bool hasTarget, Vector3 position, Quaternion rotation, float fieldOfView, int frame)
            : this(
                hasTarget,
                CameraPoseCompositor.Compose(
                    new CameraPose(position, rotation),
                    new CameraLensState(fieldOfView),
                    null),
                frame)
        {
        }

        public CameraDebugSnapshot(bool hasTarget, CameraCompositionResult composition, int frame)
        {
            HasTarget = hasTarget;
            Composition = composition;
            Frame = frame;
        }

        public bool HasTarget { get; }
        public CameraCompositionResult Composition { get; }
        public CameraPose BasePose => Composition.BasePose;
        public CameraLensState BaseLens => Composition.BaseLens;
        public IReadOnlyList<CameraLayerContribution> Contributions => Composition.Contributions;
        public Vector3 Position => Composition.FinalPose.Position;
        public Quaternion Rotation => Composition.FinalPose.Rotation;
        public float FieldOfView => Composition.FinalLens.FieldOfView;
        public int Frame { get; }
    }
}
