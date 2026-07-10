using UnityEditor;
using UnityEngine;

namespace CGame.Animation.Editor
{
    [CustomEditor(typeof(AnimationClipAsset))]
    [CanEditMultipleObjects]
    public class AnimationClipAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawNotifyEditorButton();
        }

        protected void DrawNotifyEditorButton()
        {
            using (new EditorGUI.DisabledScope(target == null))
            {
                if (GUILayout.Button("Open Notify Editor"))
                {
                    AnimationNotifyEditorWindow.Open((AnimationAssetBase)target);
                }
            }
        }
    }

    [CustomEditor(typeof(AnimationSequenceAsset))]
    [CanEditMultipleObjects]
    public class AnimationSequenceAssetEditor : AnimationClipAssetEditor
    {
    }
}
