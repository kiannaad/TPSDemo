using System;
using UnityEngine;

namespace CGame
{
    public sealed class WeaponComponent : ICombatIntentSink, IDisposable
    {
        private readonly IWeaponHitResolver hitResolver;
        private readonly string sourceEntityId;
        private readonly System.Random random;
        private readonly int magazineCapacity;
        private readonly float secondsPerShot;
        private readonly float reloadDuration;
        private readonly float damage;
        private readonly float range;
        private readonly float spreadDegrees;
        private readonly LayerMask hitMask;

        private CharacterCombatIntent combatIntent;
        private float cooldownRemaining;
        private float reloadRemaining;
        private double elapsedTime;
        private int shotSequence;
        private bool isDisposed;

        public WeaponComponent(
            WeaponProfile profile,
            IWeaponHitResolver hitResolver,
            string sourceEntityId,
            int randomSeed = 0)
        {
            if (profile == null || !profile.IsValid)
            {
                throw new ArgumentException("A valid weapon profile is required.", nameof(profile));
            }

            this.hitResolver = hitResolver ?? throw new ArgumentNullException(nameof(hitResolver));
            if (string.IsNullOrWhiteSpace(sourceEntityId))
            {
                throw new ArgumentException("A valid source entity ID is required.", nameof(sourceEntityId));
            }

            this.sourceEntityId = sourceEntityId;
            random = new System.Random(randomSeed);
            magazineCapacity = profile.MagazineCapacity;
            secondsPerShot = profile.SecondsPerShot;
            reloadDuration = profile.ReloadDuration;
            damage = profile.Damage;
            range = profile.Range;
            spreadDegrees = profile.SpreadDegrees;
            hitMask = profile.HitMask;
            AmmoInMagazine = magazineCapacity;
        }

        public int AmmoInMagazine { get; private set; }
        public bool IsReloading => reloadRemaining > 0f;
        public bool CanFire => !isDisposed && !IsReloading && cooldownRemaining <= 0f && AmmoInMagazine > 0;
        public int ShotsFired => shotSequence;
        public bool HasLastShot { get; private set; }
        public WeaponShotResult LastShot { get; private set; }

        public event Action ReloadStarted;
        public event Action ReloadCompleted;
        public event Action<WeaponShotResult> ShotFired;

        public void SubmitCombatIntent(in CharacterCombatIntent intent)
        {
            if (!isDisposed)
            {
                combatIntent = intent;
            }
        }

        public void Advance(float deltaTime, Vector3 origin)
        {
            if (isDisposed)
            {
                return;
            }

            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            elapsedTime += deltaTime;
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - deltaTime);
            AdvanceReload(deltaTime);

            if (combatIntent.ReloadRequested)
            {
                TryStartReload();
            }

            if (combatIntent.FireRequested && CanFire && combatIntent.AimDirection.sqrMagnitude > 0.000001f)
            {
                Fire(origin, combatIntent.AimDirection);
            }

            combatIntent = new CharacterCombatIntent(combatIntent.AimDirection, combatIntent.FireRequested, false);
        }

        public bool TryStartReload()
        {
            if (isDisposed || IsReloading || AmmoInMagazine >= magazineCapacity)
            {
                return false;
            }

            ReloadStarted?.Invoke();
            if (reloadDuration <= 0f)
            {
                AmmoInMagazine = magazineCapacity;
                ReloadCompleted?.Invoke();
                return true;
            }

            reloadRemaining = reloadDuration;
            return true;
        }

        public void Dispose()
        {
            isDisposed = true;
            combatIntent = default;
            cooldownRemaining = 0f;
            reloadRemaining = 0f;
            HasLastShot = false;
            LastShot = default;
        }

        private void AdvanceReload(float deltaTime)
        {
            if (!IsReloading)
            {
                return;
            }

            reloadRemaining = Mathf.Max(0f, reloadRemaining - deltaTime);
            if (reloadRemaining <= 0f)
            {
                AmmoInMagazine = magazineCapacity;
                ReloadCompleted?.Invoke();
            }
        }

        private void Fire(Vector3 origin, Vector3 aimDirection)
        {
            AmmoInMagazine--;
            cooldownRemaining = secondsPerShot;
            shotSequence++;

            Vector3 shotDirection = ApplySpread(aimDirection.normalized);
            WeaponHitResult hit = hitResolver.Resolve(origin, shotDirection, range, hitMask);
            bool damageApplied = false;
            if (hit.HasTarget)
            {
                var damageEvent = new DamageEvent(
                    $"{sourceEntityId}:{shotSequence}",
                    sourceEntityId,
                    hit.Target.EntityId,
                    damage,
                    hit.Point,
                    shotDirection,
                    elapsedTime);
                damageApplied = hit.Target.ApplyDamage(damageEvent);
            }

            LastShot = new WeaponShotResult(shotSequence, shotDirection, hit, damageApplied);
            HasLastShot = true;
            ShotFired?.Invoke(LastShot);
        }

        private Vector3 ApplySpread(Vector3 direction)
        {
            if (spreadDegrees <= 0f)
            {
                return direction;
            }

            Vector3 reference = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.99f
                ? Vector3.right
                : Vector3.up;
            Vector3 right = Vector3.Cross(reference, direction).normalized;
            Vector3 up = Vector3.Cross(direction, right).normalized;
            float radius = Mathf.Tan(spreadDegrees * Mathf.Deg2Rad);
            float x = ((float)random.NextDouble() * 2f - 1f) * radius;
            float y = ((float)random.NextDouble() * 2f - 1f) * radius;
            return (direction + right * x + up * y).normalized;
        }
    }
}
