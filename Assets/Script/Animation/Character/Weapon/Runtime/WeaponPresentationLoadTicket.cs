namespace CGame.Animation
{
    public readonly struct WeaponPresentationLoadTicket
    {
        public WeaponPresentationLoadTicket(ulong characterLifecycleToken, uint generation, ulong bindingToken)
        {
            CharacterLifecycleToken = characterLifecycleToken;
            Generation = generation;
            BindingToken = bindingToken;
        }

        public ulong CharacterLifecycleToken { get; }
        public uint Generation { get; }
        public ulong BindingToken { get; }
        public bool IsValid => CharacterLifecycleToken > 0ul && Generation > 0u && BindingToken > 0ul;
    }
}
