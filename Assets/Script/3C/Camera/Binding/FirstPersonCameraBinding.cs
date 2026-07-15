using System;
using UnityEngine;

namespace CGame
{
    public sealed class FirstPersonCameraBinding : IDisposable
    {
        private IFirstPersonCameraTarget target;

        public IFirstPersonCameraTarget Target => target != null && target.IsValid ? target : null;
        public bool IsBound => Target != null;

        public bool Bind(IFirstPersonCameraTarget cameraTarget)
        {
            if (cameraTarget == null || !cameraTarget.IsValid)
            {
                return false;
            }

            target = cameraTarget;
            return true;
        }

        public FirstPersonCameraBindResult BindCharacter(Transform characterRoot)
        {
            if (characterRoot == null)
            {
                return FirstPersonCameraBindResult.InvalidCharacter;
            }

            FirstPersonCameraAnchor anchor = characterRoot.GetComponentInChildren<FirstPersonCameraAnchor>(true);
            if (anchor == null)
            {
                return FirstPersonCameraBindResult.MissingAnchor;
            }

            return Bind(anchor)
                ? FirstPersonCameraBindResult.Bound
                : FirstPersonCameraBindResult.InvalidAnchor;
        }

        public void Unbind()
        {
            target = null;
        }

        public void Dispose()
        {
            Unbind();
        }
    }
}
