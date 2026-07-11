namespace CGame.Animation
{
    public readonly struct AnimationDebugEvent
    {
        public AnimationDebugEvent(float time, string source, string eventName, float value)
        {
            Time = time;
            Source = source ?? string.Empty;
            EventName = eventName ?? string.Empty;
            Value = value;
        }

        public float Time { get; }
        public string Source { get; }
        public string EventName { get; }
        public float Value { get; }
    }
}
