using System;

namespace CGame
{
    public sealed class ResolvedCharacterDefinitionLease : IDisposable
    {
        private Action release;

        public ResolvedCharacterDefinitionLease(CharacterDefinition definition, Action release = null)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.release = release;
        }

        public CharacterDefinition Definition { get; }
        public bool IsReleased { get; private set; }

        public void Dispose()
        {
            if (IsReleased)
            {
                return;
            }

            IsReleased = true;
            Action releaseAction = release;
            release = null;
            releaseAction?.Invoke();
        }
    }
}
