using System;
using UnityEngine;

namespace CGame
{
    public sealed class PhysicsCoverQuery
    {
        private readonly LayerMask mask;

        public PhysicsCoverQuery(LayerMask mask)
        {
            this.mask = mask;
        }

        public bool IsThreatOccluded(
            CoverSlotBehaviour slot,
            Vector3 threatPosition,
            Transform ignoredRoot)
        {
            if (slot == null)
            {
                return false;
            }

            return IsBlocked(
                threatPosition,
                slot.ProtectedPosition,
                ignoredRoot,
                0.15f);
        }

        public bool HasLineOfFire(
            CoverSlotBehaviour slot,
            Vector3 threatPosition,
            Transform ignoredRoot)
        {
            if (slot == null)
            {
                return false;
            }

            return !IsBlocked(
                slot.PeekPosition,
                threatPosition,
                ignoredRoot,
                1f);
        }

        private bool IsBlocked(
            Vector3 start,
            Vector3 end,
            Transform ignoredRoot,
            float endpointClearance)
        {
            Vector3 delta = end - start;
            float distance = delta.magnitude;
            if (distance <= 0.3f)
            {
                return false;
            }

            RaycastHit[] hits = Physics.RaycastAll(
                start,
                delta / distance,
                Mathf.Max(0f, distance - endpointClearance),
                mask,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                Transform hitTransform = hits[i].collider.transform;
                if (ignoredRoot != null && hitTransform.IsChildOf(ignoredRoot))
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
