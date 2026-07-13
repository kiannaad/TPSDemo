using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static CGame.Animation.Editor.AnimationEditorConstants;

namespace CGame.Animation.Editor
{
    public partial class AnimationEditorWindow
    {
        private void DrawPreviewPanel()
        {
            AnimationClip clip = state.EditingAsset.MainClip;
            if (clip == null)
            {
                return;
            }

            float frameRate = Mathf.Max(1f, clip.frameRate);
            int maxFrame = Mathf.Max(0, Mathf.RoundToInt(clip.length * frameRate));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Pose Preview", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            state.PreviewSource = (GameObject)EditorGUILayout.ObjectField("Preview Source", state.PreviewSource, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                RebuildPreviewInstance();
            }

            SetPreviewFrame(EditorGUILayout.IntSlider("Frame", state.PreviewFrame, 0, maxFrame));

            Rect previewRect = GUILayoutUtility.GetRect(10f, 180f, GUILayout.ExpandWidth(true));
            DrawPreviewViewport(previewRect, clip, frameRate);
        }

        private void DrawStaticPreviewPanel(Rect previewRect)
        {
            AnimationClip clip = state.EditingAsset.MainClip;
            if (clip == null)
            {
                EditorGUILayout.HelpBox("This asset has no AnimationClip. Timeline preview is disabled until a clip is assigned.", MessageType.Warning);
                return;
            }

            float frameRate = Mathf.Max(1f, clip.frameRate);
            DrawPreviewViewport(previewRect, clip, frameRate);
            DrawStaticAssetOverlay(previewRect, clip);
        }

        private void DrawStaticAssetOverlay(Rect previewRect, AnimationClip clip)
        {
            Rect labelRect = new Rect(previewRect.x + 8f, previewRect.y + 6f, previewRect.width - 16f, 42f);
            EditorGUI.DrawRect(labelRect, new Color(0f, 0f, 0f, 0.35f));
            GUI.Label(new Rect(labelRect.x + 8f, labelRect.y + 4f, labelRect.width - 16f, 18f), state.EditingAsset.name, EditorStyles.whiteBoldLabel);
            GUI.Label(new Rect(labelRect.x + 8f, labelRect.y + 22f, labelRect.width - 16f, 16f), $"{clip.name}  {clip.length:0.###}s  {clip.frameRate:0.##} fps", EditorStyles.whiteMiniLabel);
        }

        private void DrawPreviewViewport(Rect rect, AnimationClip clip, float frameRate)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            EnsurePreviewSource();
            if (state.PreviewSource == null)
            {
                GUI.Label(rect, "Preview source model could not be loaded.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            EnsurePreviewUtility();
            EnsurePreviewInstance();
            if (previewInstance == null)
            {
                GUI.Label(rect, "Preview source could not be instantiated.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            SamplePreviewFrame(clip, frameRate);
            Bounds bounds = CalculatePreviewBounds(previewInstance);
            Vector3 center = bounds.center;
            float radius = Mathf.Max(0.5f, bounds.extents.magnitude);
            previewUtility.camera.transform.position = center + new Vector3(0f, radius * 0.55f, -radius * 2.4f);
            previewUtility.camera.transform.LookAt(center + Vector3.up * radius * 0.2f);
            previewUtility.camera.nearClipPlane = 0.01f;
            previewUtility.camera.farClipPlane = radius * 8f;
            previewUtility.lights[0].intensity = 1.2f;
            previewUtility.lights[0].transform.rotation = Quaternion.Euler(35f, 35f, 0f);
            previewUtility.lights[1].intensity = 0.7f;

            previewUtility.BeginPreview(rect, GUIStyle.none);
            previewUtility.Render();
            Texture texture = previewUtility.EndPreview();
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
        }

        private void SamplePreviewFrame(AnimationClip clip, float frameRate)
        {
            float time = Mathf.Clamp(state.PreviewFrame / frameRate, 0f, clip.length);
            AnimationMode.StartAnimationMode();
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(previewInstance, clip, time);
            AnimationMode.EndSampling();
        }

        private void EnsurePreviewSource()
        {
            if (state.PreviewSource != null)
            {
                return;
            }

            GameObject defaultPreviewSource = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultPreviewSourcePath);
            if (defaultPreviewSource == null)
            {
                return;
            }

            state.PreviewSource = defaultPreviewSource;
        }

        private void EnsurePreviewUtility()
        {
            if (previewUtility != null)
            {
                return;
            }

            previewUtility = new PreviewRenderUtility();
            previewUtility.cameraFieldOfView = 30f;
        }

        private void EnsurePreviewInstance()
        {
            if (previewInstance != null || state.PreviewSource == null)
            {
                return;
            }

            RebuildPreviewInstance();
        }

        private void RebuildPreviewInstance()
        {
            DestroyPreviewInstance();
            if (state.PreviewSource == null)
            {
                return;
            }

            EnsurePreviewUtility();
            previewInstance = Instantiate(state.PreviewSource);
            previewInstance.hideFlags = HideFlags.HideAndDontSave;
            previewUtility.AddSingleGO(previewInstance);
        }

        private void DestroyPreviewInstance()
        {
            if (previewInstance == null)
            {
                return;
            }

            DestroyImmediate(previewInstance);
            previewInstance = null;
        }

        private static Bounds CalculatePreviewBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

    }
}
