namespace CGame
{
    public interface ICharacterDefinitionResolveOperation
    {
        bool IsCompleted { get; }
        CharacterDefinitionResolveResult Result { get; }
    }
}
