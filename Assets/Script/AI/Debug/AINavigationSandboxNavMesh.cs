using UnityEngine;
using UnityEngine.AI;

namespace CGame
{
    public sealed class AINavigationSandboxNavMesh : MonoBehaviour
    {
        [SerializeField]
        private NavMeshData navMeshData;

        private NavMeshDataInstance navMeshInstance;

        public NavMeshData NavMeshData => navMeshData;
        public bool IsRegistered => navMeshInstance.valid;

        public void Configure(NavMeshData configuredNavMeshData)
        {
            RemoveData();
            navMeshData = configuredNavMeshData;
            if (isActiveAndEnabled)
            {
                AddData();
            }
        }

        private void OnEnable()
        {
            AddData();
        }

        private void OnDisable()
        {
            RemoveData();
        }

        private void AddData()
        {
            if (!navMeshInstance.valid && navMeshData != null)
            {
                navMeshInstance = NavMesh.AddNavMeshData(navMeshData);
            }
        }

        private void RemoveData()
        {
            if (navMeshInstance.valid)
            {
                navMeshInstance.Remove();
            }
        }
    }
}
