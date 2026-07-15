using System;
using UnityEngine;

namespace CGame
{
    public sealed class AIPerceptionTargetBehaviour : MonoBehaviour, IAIPerceptionTarget
    {
        [SerializeField]
        private string entityId;

        [SerializeField]
        private Transform observationPoint;

        public string EntityId => entityId;
        public Vector3 Position => observationPoint != null
            ? observationPoint.position
            : transform.position;
        public Transform Transform => transform;
        public bool IsActive => isActiveAndEnabled && gameObject.activeInHierarchy;

        public void Configure(string configuredEntityId, Transform configuredObservationPoint = null)
        {
            if (string.IsNullOrWhiteSpace(configuredEntityId))
            {
                throw new ArgumentException("A valid entity ID is required.", nameof(configuredEntityId));
            }

            entityId = configuredEntityId;
            observationPoint = configuredObservationPoint;
        }
    }
}
