using System;

namespace CGame
{
    public interface ICharacterDefinitionResolveOperation : IDisposable
    {
        bool IsCompleted { get; }
        CharacterDefinitionResolveResult Result { get; }
    }
}
