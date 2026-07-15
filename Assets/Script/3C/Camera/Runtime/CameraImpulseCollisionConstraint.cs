using UnityEngine;

namespace CGame
{
    public sealed class CameraImpulseCollisionConstraint
    {
        private const float ProbeRadius = 0.012f;
        private const float SurfaceClearance = 0.002f;
        private readonly RaycastHit[] hits = new RaycastHit[32];

        public CameraPoseDelta Constrain(
            CameraPose basePose,
            CameraPoseDelta impulse,
            IFirstPersonCameraTarget target)
        {
            Vector3 localPosition = impulse.LocalPosition;
            float requestedDistance = localPosition.magnitude;
            if (impulse.Weight <= 0f || requestedDistance <= 0.000001f)
            {
                return impulse;
            }

            Vector3 worldDirection = basePose.Rotation * (localPosition / requestedDistance);
            int hitCount = Physics.SphereCastNonAlloc(
                basePose.Position,
                ProbeRadius,
                worldDirection,
                hits,
                requestedDistance,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);
            CharacterPhysicsMotor ownerMotor = target is Component component
                ? component.GetComponentInParent<CharacterPhysicsMotor>()
                : null;
            float allowedDistance = requestedDistance;
            for (int index = 0; index < hitCount; index++)
            {
                Collider collider = hits[index].collider;
                if (collider == null ||
                    (ownerMotor != null && collider.transform.IsChildOf(ownerMotor.transform)))
                {
                    continue;
                }

                allowedDistance = Mathf.Min(
                    allowedDistance,
                    Mathf.Max(0f, hits[index].distance - SurfaceClearance));
            }

            Vector3 constrainedPosition = CompressLocalPosition(localPosition, allowedDistance);
            return new CameraPoseDelta(constrainedPosition, impulse.LocalRotation, impulse.Weight);
        }

        public static Vector3 CompressLocalPosition(Vector3 requestedPosition, float allowedDistance)
        {
            float requestedDistance = requestedPosition.magnitude;
            if (requestedDistance <= 0.000001f)
            {
                return Vector3.zero;
            }

            return requestedPosition.normalized * Mathf.Clamp(
                allowedDistance,
                0f,
                requestedDistance);
        }
    }
}
