using UnityEngine;

namespace CGame
{
    public sealed class PrototypeRifleView : MonoBehaviour
    {
        [SerializeField]
        private Transform modelRoot;

        [SerializeField]
        private Transform rightHandGrip;

        [SerializeField]
        private Transform leftHandSupport;

        [SerializeField]
        private Transform muzzle;

        public Transform ModelRoot => modelRoot;
        public Transform RightHandGrip => rightHandGrip;
        public Transform LeftHandSupport => leftHandSupport;
        public Transform Muzzle => muzzle;
        public bool IsValid => modelRoot != null
            && rightHandGrip != null
            && leftHandSupport != null
            && muzzle != null;

        public void Configure(
            Transform configuredModelRoot,
            Transform configuredRightHandGrip,
            Transform configuredLeftHandSupport,
            Transform configuredMuzzle)
        {
            modelRoot = configuredModelRoot;
            rightHandGrip = configuredRightHandGrip;
            leftHandSupport = configuredLeftHandSupport;
            muzzle = configuredMuzzle;
        }
    }
}
