using System;
using System.Collections.Generic;

namespace CGame
{
    public sealed class InMemoryCharacterDefinitionProvider : ICharacterDefinitionProvider
    {
        private readonly Dictionary<CharacterDefinitionId, CharacterDefinition> definitions;

        public InMemoryCharacterDefinitionProvider(IEnumerable<CharacterDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            this.definitions = new Dictionary<CharacterDefinitionId, CharacterDefinition>();
            foreach (CharacterDefinition definition in definitions)
            {
                if (definition == null || !definition.DefinitionId.IsValid)
                {
                    continue;
                }

                if (!this.definitions.TryAdd(definition.DefinitionId, definition))
                {
                    throw new ArgumentException($"Duplicate character definition ID: {definition.DefinitionId}.", nameof(definitions));
                }
            }
        }

        public CharacterDefinitionResolveResult Resolve(CharacterDefinitionId definitionId)
        {
            if (!definitionId.IsValid)
            {
                return new CharacterDefinitionResolveResult(null, CharacterDefinitionResolveError.InvalidDefinitionId);
            }

            if (!definitions.TryGetValue(definitionId, out CharacterDefinition definition))
            {
                return new CharacterDefinitionResolveResult(null, CharacterDefinitionResolveError.DefinitionNotFound);
            }

            CharacterDefinitionResolveError validationError = definition.Validate(definitionId);
            return new CharacterDefinitionResolveResult(
                validationError == CharacterDefinitionResolveError.None ? definition : null,
                validationError);
        }
    }
}
