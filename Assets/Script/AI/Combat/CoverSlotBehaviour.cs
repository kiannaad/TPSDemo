using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGame
{
    public sealed class CoverSlotBehaviour : MonoBehaviour
    {
        private static readonly List<CoverSlotBehaviour> activeSlots = new List<CoverSlotBehaviour>();

        [SerializeField]
        private string slotId;

        [SerializeField]
        private CoverStance stance;

        [SerializeField]
        private Vector3 localPeekOffset = new Vector3(0.75f, 1.4f, 0f);

        [SerializeField, Range(0f, 1f)]
        private float exposure = 0.2f;

        public string SlotId => string.IsNullOrWhiteSpace(slotId) ? gameObject.name : slotId;
        public CoverStance Stance => stance;
        public Vector3 Position => transform.position;
        public Vector3 Facing => transform.forward;
        public Vector3 PeekPosition => transform.TransformPoint(localPeekOffset);
        public Vector3 ProtectedPosition => transform.position
            + Vector3.up * (stance == CoverStance.Crouching ? 0.9f : 1.4f);
        public float Exposure => exposure;

        public void Configure(
            string configuredSlotId,
            CoverStance configuredStance,
            Vector3 configuredLocalPeekOffset,
            float configuredExposure)
        {
            slotId = !string.IsNullOrWhiteSpace(configuredSlotId)
                ? configuredSlotId
                : throw new ArgumentException("A valid slot ID is required.", nameof(configuredSlotId));
            stance = configuredStance;
            localPeekOffset = configuredLocalPeekOffset;
            exposure = Mathf.Clamp01(configuredExposure);
        }

        public static CoverSlotBehaviour[] CopyActiveSlots()
        {
            activeSlots.RemoveAll(slot => slot == null || !slot.isActiveAndEnabled);
            return activeSlots.ToArray();
        }

        private void OnEnable()
        {
            if (!activeSlots.Contains(this))
            {
                activeSlots.Add(this);
            }
        }

        private void OnDisable()
        {
            activeSlots.Remove(this);
        }
    }
}
