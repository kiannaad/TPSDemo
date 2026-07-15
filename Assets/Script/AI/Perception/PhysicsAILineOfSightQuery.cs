using System;
using UnityEngine;

namespace CGame
{
    public sealed class PhysicsAILineOfSightQuery : IAILineOfSightQuery
    {
        private RaycastHit[] hits = new RaycastHit[16];

        public bool HasLineOfSight(
            Vector3 origin,
            Transform observerRoot,
            IAIPerceptionTarget target,
            LayerMask occlusionMask)
        {
            if (target == null || !target.IsActive || target.Transform == null)
            {
                return false;
            }

            Vector3 direction = target.Position - origin;
            float distance = direction.magnitude;
            if (distance <= 0.0001f)
            {
                return true;
            }

            int count = Physics.RaycastNonAlloc(
                origin,
                direction / distance,
                hits,
                distance + 0.01f,
                occlusionMask,
                QueryTriggerInteraction.Ignore);
            if (count == hits.Length)
            {
                Array.Resize(ref hits, hits.Length * 2);
                return HasLineOfSight(origin, observerRoot, target, occlusionMask);
            }

            Array.Sort(hits, 0, count, RaycastHitDistanceComparer.Instance);
            for (int i = 0; i < count; i++)
            {
                Transform hitTransform = hits[i].transform;
                if (hitTransform == null)
                {
                    continue;
                }

                if (observerRoot != null
                    && (hitTransform == observerRoot || hitTransform.IsChildOf(observerRoot)))
                {
                    continue;
                }

                return hitTransform == target.Transform
                    || hitTransform.IsChildOf(target.Transform)
                    || target.Transform.IsChildOf(hitTransform);
            }

            return true;
        }

        private sealed class RaycastHitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();

            public int Compare(RaycastHit left, RaycastHit right)
            {
                return left.distance.CompareTo(right.distance);
            }
        }
    }
}
