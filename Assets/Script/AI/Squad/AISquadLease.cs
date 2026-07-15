namespace CGame
{
    public sealed class AISquadLease
    {
        private AISquadContext context;

        internal AISquadLease(
            AISquadContext ownerContext,
            AISquadResourceKind kind,
            string resourceId,
            string ownerId,
            double expiresAt)
        {
            context = ownerContext;
            Kind = kind;
            ResourceId = resourceId;
            OwnerId = ownerId;
            ExpiresAt = expiresAt;
        }

        public AISquadResourceKind Kind { get; }
        public string ResourceId { get; }
        public string OwnerId { get; }
        public double ExpiresAt { get; }
        public bool IsActive => context != null && context.IsLeaseActive(this);

        public bool Release()
        {
            AISquadContext current = context;
            if (current == null || !current.Release(this))
            {
                context = null;
                return false;
            }

            context = null;
            return true;
        }

        internal void Invalidate()
        {
            context = null;
        }
    }
}
