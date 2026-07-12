namespace CGame
{
    public readonly struct CharacterSpawnRequest
    {
        public CharacterSpawnRequest(CharacterSpawnRequestId requestId, CharacterDefinitionId definitionId, CharacterControlKind controlKind, CharacterSpawnPlacement placement, InputHandle input, string displayName)
        {
            RequestId = requestId;
            DefinitionId = definitionId;
            ControlKind = controlKind;
            Placement = placement;
            Input = input;
            DisplayName = displayName;
        }

        public CharacterSpawnRequestId RequestId { get; }
        public CharacterDefinitionId DefinitionId { get; }
        public CharacterControlKind ControlKind { get; }
        public CharacterSpawnPlacement Placement { get; }
        public InputHandle Input { get; }
        public string DisplayName { get; }
    }
}
