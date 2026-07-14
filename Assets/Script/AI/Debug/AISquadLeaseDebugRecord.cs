namespace CGame
{
    public readonly struct AISquadLeaseDebugRecord
    {
        public AISquadLeaseDebugRecord(
            AISquadResourceKind kind,
            string resourceId,
            string ownerId,
            double expiresAt)
        {
            Kind = kind;
            ResourceId = resourceId ?? string.Empty;
            OwnerId = ownerId ?? string.Empty;
            ExpiresAt = expiresAt;
        }

        public AISquadResourceKind Kind { get; }
        public string ResourceId { get; }
        public string OwnerId { get; }
        public double ExpiresAt { get; }
    }
}
