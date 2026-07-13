using System;

namespace CGame
{
    public interface ICharacterDefinitionAssetLoadOperation : IDisposable
    {
        bool IsCompleted { get; }
        bool IsSuccessful { get; }
        CharacterDefinition Asset { get; }
        string Error { get; }
    }
}
