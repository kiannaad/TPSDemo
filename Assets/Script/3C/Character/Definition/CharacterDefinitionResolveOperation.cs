using System;

namespace CGame
{
    public sealed class CharacterDefinitionResolveOperation : ICharacterDefinitionResolveOperation
    {
        public bool IsCompleted { get; private set; }
        public bool IsDisposed { get; private set; }
        public CharacterDefinitionResolveResult Result { get; private set; }

        public static CharacterDefinitionResolveOperation Completed(CharacterDefinitionResolveResult result)
        {
            var operation = new CharacterDefinitionResolveOperation();
            operation.Complete(result);
            return operation;
        }

        public void Complete(CharacterDefinitionResolveResult result)
        {
            if (IsDisposed)
            {
                result.Lease?.Dispose();
                return;
            }

            if (IsCompleted)
            {
                throw new InvalidOperationException("A definition resolve operation can only complete once.");
            }

            Result = result;
            IsCompleted = true;
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            if (IsCompleted)
            {
                Result.Lease?.Dispose();
            }
        }
    }
}
