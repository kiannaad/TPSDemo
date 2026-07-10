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
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("animationClip"));
            }

            DrawPropertiesExcluding(serializedObject, "m_Script", "animationClip");
            serializedObject.ApplyModifiedProperties();
            DrawNotifyEditorButton();
        }

        protected void DrawNotifyEditorButton()
        {
            using (new EditorGUI.DisabledScope(target == null))
            {
                if (GUILayout.Button("Open Animation Editor"))
                {
                    AnimationEditorWindow.Open((AnimationAssetBase)target);
                }
            }
        }
    }

    [CustomEditor(typeof(AnimationSequenceAsset))]
    [CanEditMultipleObjects]
    public class AnimationSequenceAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawNotifyEditorButton();
        }

        private void DrawNotifyEditorButton()
        {
            using (new EditorGUI.DisabledScope(target == null))
            {
                if (GUILayout.Button("Open Animation Editor"))
                {
                    AnimationEditorWindow.Open((AnimationAssetBase)target);
                }
            }
        }
    }
}
