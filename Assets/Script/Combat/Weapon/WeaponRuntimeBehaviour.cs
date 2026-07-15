using System;
using UnityEngine;

namespace CGame
{
    public sealed class WeaponRuntimeBehaviour : MonoBehaviour, ICombatIntentSink
    {
        private WeaponComponent weapon;
        private Transform muzzle;
        private Transform weaponRoot;

        public WeaponComponent Weapon => weapon;
        public Transform Muzzle => muzzle;
        public bool IsInitialized => weapon != null && muzzle != null;

        public void Initialize(
            WeaponProfile profile,
            Transform configuredMuzzle,
            Transform configuredWeaponRoot,
            string sourceEntityId,
            int randomSeed = 0)
        {
            if (weapon != null)
            {
                throw new InvalidOperationException("Weapon runtime is already initialized.");
            }

            muzzle = configuredMuzzle != null
                ? configuredMuzzle
                : throw new ArgumentNullException(nameof(configuredMuzzle));
            weaponRoot = configuredWeaponRoot != null
                ? configuredWeaponRoot
                : throw new ArgumentNullException(nameof(configuredWeaponRoot));
            weapon = new WeaponComponent(
                profile,
                new PhysicsWeaponHitResolver(),
                sourceEntityId,
                randomSeed);
        }

        public void SubmitCombatIntent(in CharacterCombatIntent intent)
        {
            if (weapon == null)
            {
                return;
            }

            if (intent.AimDirection.sqrMagnitude > 0.000001f && weaponRoot != null)
            {
                weaponRoot.rotation = Quaternion.LookRotation(intent.AimDirection, Vector3.up);
            }

            weapon.SubmitCombatIntent(intent);
        }

        public void Advance(float deltaTime)
        {
            if (weapon != null && muzzle != null)
            {
                weapon.Advance(deltaTime, muzzle.position);
            }
        }

        public void Shutdown()
        {
            weapon?.Dispose();
            weapon = null;
            muzzle = null;
            weaponRoot = null;
            enabled = false;
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        private void OnDestroy()
        {
            Shutdown();
        }
    }
}
