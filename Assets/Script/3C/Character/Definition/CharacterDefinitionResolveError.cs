namespace CGame
{
    public enum CharacterDefinitionResolveError
    {
        None,
        InvalidDefinitionId,
        DefinitionNotFound,
        DefinitionIdMismatch,
        MissingVisualPrefab,
        InvalidAnimationConfig,
        MissingSupportedControlKind,
    }
}
