namespace CGame
{
    public readonly struct CharacterDefinitionResolveResult
    {
        public CharacterDefinitionResolveResult(CharacterDefinition definition, CharacterDefinitionResolveError error)
            : this(definition == null ? null : new ResolvedCharacterDefinitionLease(definition), error)
        {
        }

        public CharacterDefinitionResolveResult(ResolvedCharacterDefinitionLease lease, CharacterDefinitionResolveError error)
        {
            Lease = lease;
            Error = error;
        }

        public ResolvedCharacterDefinitionLease Lease { get; }
        public CharacterDefinition Definition => Lease?.Definition;
        public CharacterDefinitionResolveError Error { get; }
        public bool IsSuccess => Lease != null && Definition != null && Error == CharacterDefinitionResolveError.None;
    }
}
