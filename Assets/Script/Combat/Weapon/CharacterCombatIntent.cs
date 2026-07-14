using UnityEngine;

namespace CGame
{
    public readonly struct CharacterCombatIntent
    {
        public CharacterCombatIntent(Vector3 aimDirection, bool fireRequested, bool reloadRequested)
        {
            AimDirection = aimDirection.sqrMagnitude > 0.000001f
                ? aimDirection.normalized
                : Vector3.zero;
            FireRequested = fireRequested;
            ReloadRequested = reloadRequested;
        }

        public Vector3 AimDirection { get; }
        public bool FireRequested { get; }
        public bool ReloadRequested { get; }
    }
}
