namespace CGame
{
    public interface ICharacterDefinitionProvider
    {
        CharacterDefinitionResolveResult Resolve(CharacterDefinitionId definitionId);
    }
}
