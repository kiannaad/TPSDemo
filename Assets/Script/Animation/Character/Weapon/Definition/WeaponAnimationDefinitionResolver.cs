using System;
using System.Collections.Generic;

namespace CGame.Animation
{
    public sealed class WeaponAnimationDefinitionResolver
    {
        private readonly Dictionary<WeaponId, WeaponAnimationDefinition> definitions = new Dictionary<WeaponId, WeaponAnimationDefinition>();

        public WeaponAnimationDefinitionResolver(IEnumerable<WeaponAnimationDefinition> definitions)
        {
            if (definitions == null)
            {
                return;
            }

            foreach (WeaponAnimationDefinition definition in definitions)
            {
                if (definition == null || !definition.IsValid)
                {
                    continue;
                }

                if (!this.definitions.TryAdd(definition.WeaponId, definition))
                {
                    throw new ArgumentException($"Duplicate weapon animation definition: {definition.WeaponId}", nameof(definitions));
                }
            }
        }

        public bool TryResolve(WeaponId weaponId, out WeaponAnimationDefinition definition)
        {
            if (!weaponId.IsValid)
            {
                definition = null;
                return false;
            }

            return definitions.TryGetValue(weaponId, out definition);
        }
    }
}
