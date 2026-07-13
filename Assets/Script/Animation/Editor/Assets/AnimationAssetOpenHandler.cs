using UnityEditor;
using UnityEditor.Callbacks;

namespace CGame.Animation.Editor
{
    public static class AnimationAssetOpenHandler
    {
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not AnimationAssetBase asset)
            {
                return false;
            }

            AnimationEditorWindow.Open(asset);
            return true;
        }
    }
}
