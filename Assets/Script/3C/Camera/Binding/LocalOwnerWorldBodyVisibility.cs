using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGame
{
    public sealed class LocalOwnerWorldBodyVisibility : IDisposable
    {
        private readonly Camera worldCamera;
        private readonly int ownerWorldBodyLayer;
        private readonly int originalWorldCameraCullingMask;
        private readonly Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();
        private bool isDisposed;

        public LocalOwnerWorldBodyVisibility(Camera worldCamera, int ownerWorldBodyLayer)
        {
            this.worldCamera = worldCamera != null
                ? worldCamera
                : throw new ArgumentNullException(nameof(worldCamera));
            if (ownerWorldBodyLayer < 0 || ownerWorldBodyLayer > 31)
            {
                throw new ArgumentOutOfRangeException(nameof(ownerWorldBodyLayer));
            }

            this.ownerWorldBodyLayer = ownerWorldBodyLayer;
            originalWorldCameraCullingMask = worldCamera.cullingMask;
            worldCamera.cullingMask &= ~(1 << ownerWorldBodyLayer);
        }

        public bool IsBound => originalLayers.Count > 0;
        public int AffectedRendererCount { get; private set; }

        public bool BindCharacter(Transform characterRoot)
        {
            if (isDisposed)
            {
                return false;
            }

            Unbind();
            if (characterRoot == null)
            {
                return false;
            }

            Renderer[] renderers = characterRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return false;
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.GetComponent<Collider>() != null)
                {
                    return false;
                }
            }

            foreach (Renderer renderer in renderers)
            {
                GameObject rendererObject = renderer.gameObject;
                if (!originalLayers.ContainsKey(rendererObject))
                {
                    originalLayers.Add(rendererObject, rendererObject.layer);
                    rendererObject.layer = ownerWorldBodyLayer;
                }
            }

            AffectedRendererCount = renderers.Length;
            return true;
        }

        public void Unbind()
        {
            foreach (KeyValuePair<GameObject, int> pair in originalLayers)
            {
                if (pair.Key != null)
                {
                    pair.Key.layer = pair.Value;
                }
            }

            originalLayers.Clear();
            AffectedRendererCount = 0;
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            Unbind();
            if (worldCamera != null)
            {
                worldCamera.cullingMask = originalWorldCameraCullingMask;
            }
        }
    }
}
