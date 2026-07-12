namespace CGame
{
    public sealed class CharacterSpawnOperation
    {
        internal CharacterSpawnOperation(CharacterSpawnRequest request)
        {
            Request = request;
            State = CharacterSpawnState.Requested;
        }

        public CharacterSpawnRequest Request { get; }
        public CharacterSpawnState State { get; internal set; }
        public CharacterRuntimeId RuntimeId { get; internal set; }
        public CharacterDefinitionResolveError Error { get; internal set; }
        public bool IsComplete => State == CharacterSpawnState.CharacterReady || State == CharacterSpawnState.Failed;
    }
}
