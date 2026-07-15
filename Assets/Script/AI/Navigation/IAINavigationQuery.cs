using UnityEngine;

namespace CGame
{
    public interface IAINavigationQuery
    {
        AINavigationPathResult CalculatePath(Vector3 start, Vector3 destination);
        AINavigationPathResult Cancel();
    }
}
