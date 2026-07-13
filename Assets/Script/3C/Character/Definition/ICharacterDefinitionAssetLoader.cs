namespace CGame
{
    public interface ICharacterDefinitionAssetLoader
    {
        ICharacterDefinitionAssetLoadOperation BeginLoad(string location);
    }
}
