using System;

namespace CGame
{
    public sealed class CharacterDefinitionResolveOperation : ICharacterDefinitionResolveOperation
    {
        public bool IsCompleted { get; private set; }
        public CharacterDefinitionResolveResult Result { get; private set; }

        public void Complete(CharacterDefinitionResolveResult result)
        {
            if (IsCompleted)
            {
                throw new InvalidOperationException("A definition resolve operation can only complete once.");
            }

            Result = result;
            IsCompleted = true;
        }
    }
}
