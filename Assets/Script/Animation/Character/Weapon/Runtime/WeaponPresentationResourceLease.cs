using System;

namespace CGame.Animation
{
    public sealed class WeaponPresentationResourceLease : IWeaponPresentationResourceLease
    {
        private Action release;

        public WeaponPresentationResourceLease(WeaponAnimationDefinition definition, string definitionId, Action release = null)
        {
            Definition = definition;
            DefinitionId = string.IsNullOrWhiteSpace(definitionId) ? "<unknown>" : definitionId;
            this.release = release;
        }

        public WeaponAnimationDefinition Definition { get; }
        public string DefinitionId { get; }
        public bool IsReleased { get; private set; }

        public void Dispose()
        {
            if (IsReleased)
            {
                return;
            }

            IsReleased = true;
            Action callback = release;
            release = null;
            callback?.Invoke();
        }
    }
}
