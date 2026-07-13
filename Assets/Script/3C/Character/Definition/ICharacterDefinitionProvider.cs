namespace CGame
{
    public interface ICharacterDefinitionProvider
    {
        ICharacterDefinitionResolveOperation BeginResolve(CharacterDefinitionId definitionId);
        CharacterDefinitionResolveResult Resolve(CharacterDefinitionId definitionId);
    }
}
