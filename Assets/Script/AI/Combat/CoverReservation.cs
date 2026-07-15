namespace CGame
{
    public sealed class CoverReservation
    {
        private CoverReservationService service;

        internal CoverReservation(CoverReservationService service, string slotId, string ownerId)
        {
            this.service = service;
            SlotId = slotId;
            OwnerId = ownerId;
        }

        public string SlotId { get; }
        public string OwnerId { get; }
        public bool IsActive => service != null;

        public bool Release()
        {
            if (service == null)
            {
                return false;
            }

            CoverReservationService activeService = service;
            service = null;
            return activeService.Release(SlotId, OwnerId);
        }
    }
}
