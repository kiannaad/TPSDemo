using UnityEngine;

namespace CGame
{
    public interface IAILineOfSightQuery
    {
        bool HasLineOfSight(
            Vector3 origin,
            Transform observerRoot,
            IAIPerceptionTarget target,
            LayerMask occlusionMask);
    }
}
