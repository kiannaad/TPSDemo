using System.IO;
using UnityEditor;
using UnityEngine;

namespace CGame.Animation.Editor
{
    public static class AnimationClipAssetFactory
    {
        private const string CreateFromClipMenuPath = "Assets/CGame/Create Animation Clip Asset";

        [MenuItem(CreateFromClipMenuPath, false, 20)]
        public static void CreateFromSelectedClip()
        {
            if (Selection.activeObject is not AnimationClip clip)
            {
                return;
            }

            CreateFromClip(clip);
        }

        [MenuItem(CreateFromClipMenuPath, true)]
        private static bool CanCreateFromSelectedClip()
        {
            return Selection.activeObject is AnimationClip;
        }

        public static AnimationClipAsset CreateFromClip(AnimationClip clip)
        {
            if (clip == null)
            {
                return null;
            }

            string sourcePath = AssetDatabase.GetAssetPath(clip);
            string directory = string.IsNullOrEmpty(sourcePath)
                ? "Assets"
                : Path.GetDirectoryName(sourcePath)?.Replace('\\', '/') ?? "Assets";
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{clip.name}ClipAsset.asset");

            AnimationClipAsset asset = ScriptableObject.CreateInstance<AnimationClipAsset>();
            asset.TryInitialize(clip);
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            return asset;
        }
    }
}
