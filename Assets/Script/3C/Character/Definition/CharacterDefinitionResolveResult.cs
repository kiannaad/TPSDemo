namespace CGame
{
    public readonly struct CharacterDefinitionResolveResult
    {
        public CharacterDefinitionResolveResult(CharacterDefinition definition, CharacterDefinitionResolveError error)
        {
            Definition = definition;
            Error = error;
        }

        public CharacterDefinition Definition { get; }
        public CharacterDefinitionResolveError Error { get; }
        public bool IsSuccess => Definition != null && Error == CharacterDefinitionResolveError.None;
    }
}
