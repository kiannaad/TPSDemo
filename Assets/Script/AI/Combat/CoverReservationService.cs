using System;
using System.Collections.Generic;

namespace CGame
{
    public sealed class CoverReservationService
    {
        private static readonly CoverReservationService shared = new CoverReservationService();
        private readonly Dictionary<string, string> owners = new Dictionary<string, string>();

        public static CoverReservationService Shared => shared;

        public bool TryReserve(string slotId, string ownerId, out CoverReservation reservation)
        {
            reservation = null;
            if (string.IsNullOrWhiteSpace(slotId) || string.IsNullOrWhiteSpace(ownerId))
            {
                return false;
            }

            if (owners.TryGetValue(slotId, out string existingOwner))
            {
                return false;
            }

            owners.Add(slotId, ownerId);
            reservation = new CoverReservation(this, slotId, ownerId);
            return true;
        }

        public bool IsReserved(string slotId)
        {
            return !string.IsNullOrWhiteSpace(slotId) && owners.ContainsKey(slotId);
        }

        public bool IsReservedByOther(string slotId, string ownerId)
        {
            return owners.TryGetValue(slotId, out string existingOwner)
                && !string.Equals(existingOwner, ownerId, StringComparison.Ordinal);
        }

        public bool Release(string slotId, string ownerId)
        {
            if (!owners.TryGetValue(slotId, out string existingOwner)
                || !string.Equals(existingOwner, ownerId, StringComparison.Ordinal))
            {
                return false;
            }

            return owners.Remove(slotId);
        }
    }
}
