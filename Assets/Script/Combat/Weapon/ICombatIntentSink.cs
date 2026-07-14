namespace CGame
{
    public interface ICombatIntentSink
    {
        void SubmitCombatIntent(in CharacterCombatIntent intent);
    }
}
