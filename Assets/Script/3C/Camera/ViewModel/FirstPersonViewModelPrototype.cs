using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CGame
{
    public sealed class FirstPersonViewModelPrototype : IDisposable
    {
        private readonly Mesh boxMesh;
        private readonly List<Material> materials;
        private bool isDisposed;

        private FirstPersonViewModelPrototype(GameObject root, Mesh boxMesh, List<Material> materials, int rendererCount)
        {
            Root = root;
            this.boxMesh = boxMesh;
            this.materials = materials;
            RendererCount = rendererCount;
        }

        public GameObject Root { get; }
        public int RendererCount { get; }

        public void ApplyAdsPresentation(float adsProgress, Vector3 adsLocalPosition)
        {
            ApplyPresentation(adsProgress, adsLocalPosition, CameraPoseDelta.None);
        }

        public void ApplyPresentation(
            float adsProgress,
            Vector3 adsLocalPosition,
            CameraPoseDelta viewModelRecoil)
        {
            float progress = Mathf.Clamp01(adsProgress);
            Root.transform.localPosition =
                Vector3.Lerp(Vector3.zero, adsLocalPosition, progress) +
                viewModelRecoil.LocalPosition * viewModelRecoil.Weight;
            Root.transform.localRotation = Quaternion.Slerp(
                Quaternion.identity,
                viewModelRecoil.LocalRotation,
                viewModelRecoil.Weight);
        }

        public static FirstPersonViewModelPrototype Create(Transform parent, int viewModelLayer)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (viewModelLayer < 0 || viewModelLayer > 31)
            {
                throw new ArgumentOutOfRangeException(nameof(viewModelLayer));
            }

            var root = new GameObject("First Person ViewModel Prototype");
            root.layer = viewModelLayer;
            root.transform.SetParent(parent, false);

            Mesh mesh = CreateBoxMesh();
            var ownedMaterials = new List<Material>();
            Material gloveMaterial = CreateMaterial("ViewModel Gloves", new Color(0.12f, 0.16f, 0.13f, 1f));
            Material weaponMaterial = CreateMaterial("ViewModel Weapon", new Color(0.08f, 0.09f, 0.1f, 1f));
            Material accentMaterial = CreateMaterial("ViewModel Weapon Accent", new Color(0.28f, 0.32f, 0.25f, 1f));
            Material lensMaterial = CreateTransparentMaterial("ViewModel Sight Lens", new Color(0.18f, 0.65f, 0.72f, 0.32f));
            ownedMaterials.Add(gloveMaterial);
            ownedMaterials.Add(weaponMaterial);
            ownedMaterials.Add(accentMaterial);
            ownedMaterials.Add(lensMaterial);

            int rendererCount = 0;
            rendererCount += CreatePart(root.transform, "Left Arm", viewModelLayer, mesh, gloveMaterial,
                new Vector3(-0.02f, -0.32f, 1.02f), new Vector3(0.065f, 0.06f, 0.26f), new Vector3(-12f, -5f, 0f));
            rendererCount += CreatePart(root.transform, "Right Arm", viewModelLayer, mesh, gloveMaterial,
                new Vector3(0.16f, -0.32f, 0.92f), new Vector3(0.065f, 0.06f, 0.24f), new Vector3(-12f, 5f, 0f));
            rendererCount += CreatePart(root.transform, "Left Hand", viewModelLayer, mesh, gloveMaterial,
                new Vector3(0.03f, -0.27f, 1.15f), new Vector3(0.075f, 0.07f, 0.18f), new Vector3(0f, -5f, 0f));
            rendererCount += CreatePart(root.transform, "Right Hand", viewModelLayer, mesh, gloveMaterial,
                new Vector3(0.14f, -0.27f, 1.05f), new Vector3(0.075f, 0.07f, 0.16f), new Vector3(0f, 5f, 0f));
            rendererCount += CreatePart(root.transform, "Rifle Body", viewModelLayer, mesh, weaponMaterial,
                new Vector3(0.11f, -0.21f, 1.2f), new Vector3(0.07f, 0.08f, 0.45f), Vector3.zero);
            rendererCount += CreatePart(root.transform, "Rifle Handguard", viewModelLayer, mesh, accentMaterial,
                new Vector3(0.11f, -0.19f, 1.49f), new Vector3(0.06f, 0.06f, 0.26f), Vector3.zero);
            rendererCount += CreatePart(root.transform, "Rifle Barrel", viewModelLayer, mesh, weaponMaterial,
                new Vector3(0.11f, -0.16f, 1.72f), new Vector3(0.022f, 0.022f, 0.2f), Vector3.zero);
            rendererCount += CreatePart(root.transform, "Sight Housing", viewModelLayer, mesh, weaponMaterial,
                new Vector3(0.11f, -0.12f, 1.2f), new Vector3(0.05f, 0.06f, 0.07f), Vector3.zero);
            rendererCount += CreatePart(root.transform, "Sight Lens", viewModelLayer, mesh, lensMaterial,
                new Vector3(0.11f, -0.11f, 1.17f), new Vector3(0.038f, 0.038f, 0.008f), Vector3.zero);

            return new FirstPersonViewModelPrototype(root, mesh, ownedMaterials, rendererCount);
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            DestroyOwnedObject(Root);
            DestroyOwnedObject(boxMesh);
            foreach (Material material in materials)
            {
                DestroyOwnedObject(material);
            }
        }

        private static int CreatePart(
            Transform parent,
            string name,
            int layer,
            Mesh mesh,
            Material material,
            Vector3 localPosition,
            Vector3 localScale,
            Vector3 localEulerAngles)
        {
            var part = new GameObject(name);
            part.layer = layer;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(localEulerAngles);
            part.transform.localScale = localScale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return 1;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("The Universal Render Pipeline/Lit shader is required for the ViewModel prototype.");
            }

            var material = new Material(shader)
            {
                name = name,
                color = color
            };
            material.SetFloat("_Smoothness", 0.32f);
            return material;
        }

        private static Material CreateTransparentMaterial(string name, Color color)
        {
            Material material = CreateMaterial(name, color);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        private static Mesh CreateBoxMesh()
        {
            var mesh = new Mesh { name = "ViewModel Prototype Box" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f)
            };
            mesh.triangles = new[]
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 4, 7, 0, 7, 3,
                1, 2, 6, 1, 6, 5,
                3, 7, 6, 3, 6, 2,
                0, 1, 5, 0, 5, 4
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void DestroyOwnedObject(UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(value);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
        }
    }
}
