namespace CGame
{
    public readonly struct AIAlertTransitionRecord
    {
        public AIAlertTransitionRecord(
            AIAlertState previous,
            AIAlertState current,
            double timestamp,
            string reason)
        {
            Previous = previous;
            Current = current;
            Timestamp = timestamp;
            Reason = reason ?? string.Empty;
        }

        public AIAlertState Previous { get; }
        public AIAlertState Current { get; }
        public double Timestamp { get; }
        public string Reason { get; }
    }
}
