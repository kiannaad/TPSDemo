using UnityEngine;

namespace CGame
{
    public sealed class WeaponPresentationInstance : MonoBehaviour
    {
        [SerializeField] private Transform rightHandMount;
        [SerializeField] private Transform leftHandGrip;
        [SerializeField] private Transform muzzle;
        [SerializeField] private WeaponModelActionPlayer modelActionPlayer;

        public Transform RightHandMount => rightHandMount;
        public Transform LeftHandGrip => leftHandGrip;
        public Transform Muzzle => muzzle;
        public WeaponModelActionPlayer ModelActionPlayer => modelActionPlayer;
        public bool HasRequiredMount => rightHandMount != null;

        public WeaponPresentationBinding CreateBinding(uint generation)
        {
            return new WeaponPresentationBinding(generation, this, leftHandGrip, muzzle);
        }

        public bool AttachTo(Transform rightHand)
        {
            if (rightHand == null || rightHandMount == null)
            {
                return false;
            }

            transform.SetParent(rightHand, false);
            Quaternion inverseMountRotation = Quaternion.Inverse(rightHandMount.localRotation);
            transform.localRotation = inverseMountRotation;
            transform.localPosition = inverseMountRotation * -rightHandMount.localPosition;
            return true;
        }
    }
}
