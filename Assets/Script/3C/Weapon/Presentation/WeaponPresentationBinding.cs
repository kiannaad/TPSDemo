using UnityEngine;

namespace CGame
{
    public sealed class WeaponPresentationBinding
    {
        private readonly WeaponPresentationInstance instance;

        public WeaponPresentationBinding(
            uint generation,
            WeaponPresentationInstance instance,
            Transform leftHandGrip,
            Transform muzzle)
        {
            Generation = generation;
            this.instance = instance;
            LeftHandGrip = leftHandGrip;
            Muzzle = muzzle;
        }

        public uint Generation { get; }
        public Transform LeftHandGrip { get; }
        public Transform Muzzle { get; }
        public bool IsAlive => instance != null && instance.gameObject != null;
        public bool HasLeftHandGrip => IsAlive && LeftHandGrip != null;

        public bool CanConsume(uint generation)
        {
            return generation == Generation && HasLeftHandGrip;
        }
    }
}
