using System;
using UnityEditor;
using UnityEngine;

namespace CGame.Animation.Editor
{
    public readonly struct TimelineEventGUIState
    {
        public TimelineEventGUIState(bool hovered, bool active, bool selected)
        {
            Hovered = hovered;
            Active = active;
            Selected = selected;
        }

        public bool Hovered { get; }
        public bool Active { get; }
        public bool Selected { get; }
    }

    public readonly struct TimelineEventVisual
    {
        public TimelineEventVisual(Rect rect, string label, bool durationEvent, Color eventColor, Color swatchColor, TimelineEventGUIState state)
        {
            Rect = rect;
            Label = label;
            DurationEvent = durationEvent;
            EventColor = eventColor;
            SwatchColor = swatchColor;
            State = state;
        }

        public Rect Rect { get; }
        public string Label { get; }
        public bool DurationEvent { get; }
        public Color EventColor { get; }
        public Color SwatchColor { get; }
        public TimelineEventGUIState State { get; }
    }

    public readonly struct TimelineEventEditValues
    {
        public TimelineEventEditValues(float beginTime, int startFrame, int endFrame, float minTriggerWeight)
        {
            BeginTime = beginTime;
            StartFrame = startFrame;
            EndFrame = endFrame;
            MinTriggerWeight = minTriggerWeight;
        }

        public float BeginTime { get; }
        public int StartFrame { get; }
        public int EndFrame { get; }
        public float MinTriggerWeight { get; }
    }

    public readonly struct TimelineEventEditActions
    {
        public TimelineEventEditActions(Action<float> setBeginTime, Action<int> setStartFrame, Action<int> setEndFrame, Action<float> setMinTriggerWeight, Action delete)
        {
            SetBeginTime = setBeginTime;
            SetStartFrame = setStartFrame;
            SetEndFrame = setEndFrame;
            SetMinTriggerWeight = setMinTriggerWeight;
            Delete = delete;
        }

        public Action<float> SetBeginTime { get; }
        public Action<int> SetStartFrame { get; }
        public Action<int> SetEndFrame { get; }
        public Action<float> SetMinTriggerWeight { get; }
        public Action Delete { get; }
    }

    public static class TimelineEventGUI
    {
        private const float InstantEventTextLeftPadding = 12f;
        private const float InstantEventTextRightPadding = 8f;
        private const float EventOriginSize = 6f;
        private static readonly Color EventContextPanelColor = new Color(0.16f, 0.16f, 0.16f, 0.98f);
        private static readonly Color EventContextBorderColor = new Color(0.34f, 0.34f, 0.34f, 1f);
        private static readonly Color EventContextInputColor = new Color(0.055f, 0.055f, 0.055f, 1f);
        private static readonly Color EventContextLabelColor = new Color(0.78f, 0.78f, 0.78f, 1f);

        public const float ContextPanelWidth = 260f;
        public const float ContextPanelHeight = 176f;

        public static float GetInstantEventWidth(string label)
        {
            float labelWidth = EditorStyles.whiteMiniLabel.CalcSize(new GUIContent(label)).x;
            return Mathf.Ceil(labelWidth + InstantEventTextLeftPadding + InstantEventTextRightPadding);
        }

        public static void DrawEvent(TimelineEventVisual visual)
        {
            EditorGUI.DrawRect(visual.Rect, visual.EventColor);
            if (visual.State.Hovered || visual.State.Active || visual.State.Selected)
            {
                EditorGUI.DrawRect(visual.Rect, visual.State.Active || visual.State.Selected ? new Color(1f, 1f, 1f, 0.22f) : new Color(1f, 1f, 1f, 0.12f));
            }

            EditorGUI.DrawRect(new Rect(visual.Rect.x, visual.Rect.yMax - 3f, visual.Rect.width, 3f), visual.SwatchColor);
            DrawBorder(visual.Rect, visual.State.Active || visual.State.Selected ? Color.white : new Color(0f, 0f, 0f, visual.State.Hovered ? 0.65f : 0.45f));

            if (visual.DurationEvent)
            {
                DrawDurationEventOrigins(visual.Rect, visual.SwatchColor);
            }
            else
            {
                DrawInstantEventOrigin(visual.Rect, visual.SwatchColor);
            }

            Rect textRect = new Rect(
                visual.Rect.x + (visual.DurationEvent ? 6f : InstantEventTextLeftPadding),
                visual.Rect.y + 3f,
                Mathf.Max(1f, visual.Rect.width - (visual.DurationEvent ? 12f : InstantEventTextLeftPadding + InstantEventTextRightPadding)),
                16f);
            if (textRect.width > 18f)
            {
                GUI.Label(textRect, visual.Label, EditorStyles.whiteMiniLabel);
            }
        }

        public static bool DrawContextPanel(Rect panelRect, TimelineEventEditValues values, bool durationEvent, TimelineEventEditActions actions)
        {
            EditorGUI.DrawRect(panelRect, EventContextPanelColor);
            DrawBorder(panelRect, EventContextBorderColor);
            bool deleteRequested = false;
            GUILayout.BeginArea(new Rect(panelRect.x + 10f, panelRect.y + 8f, panelRect.width - 20f, panelRect.height - 16f));
            DrawContextHeader("NOTIFY");

            EditorGUI.BeginChangeCheck();
            float beginTime = DrawContextFloatField("Begin Time", values.BeginTime);
            if (EditorGUI.EndChangeCheck())
            {
                actions.SetBeginTime?.Invoke(beginTime);
            }

            EditorGUI.BeginChangeCheck();
            int startFrame = DrawContextIntField(durationEvent ? "Start Frame" : "Notify Frame", values.StartFrame);
            if (EditorGUI.EndChangeCheck())
            {
                actions.SetStartFrame?.Invoke(startFrame);
            }

            if (durationEvent)
            {
                EditorGUI.BeginChangeCheck();
                int endFrame = DrawContextIntField("End Frame", values.EndFrame);
                if (EditorGUI.EndChangeCheck())
                {
                    actions.SetEndFrame?.Invoke(endFrame);
                }
            }

            EditorGUI.BeginChangeCheck();
            float minTriggerWeight = DrawContextFloatField("Min Trigger Weight", values.MinTriggerWeight);
            if (EditorGUI.EndChangeCheck())
            {
                actions.SetMinTriggerWeight?.Invoke(minTriggerWeight);
            }

            DrawContextHeader("EDIT");
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Delete", EditorStyles.miniButton, GUILayout.Width(84f)))
                {
                    deleteRequested = true;
                }
            }

            GUILayout.EndArea();
            if (deleteRequested)
            {
                actions.Delete?.Invoke();
            }

            return deleteRequested;
        }

        public static void DrawBorder(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
        }

        private static void DrawInstantEventOrigin(Rect eventRect, Color color)
        {
            DrawEventOrigin(new Vector2(eventRect.x, eventRect.center.y), color);
        }

        private static void DrawDurationEventOrigins(Rect eventRect, Color color)
        {
            DrawEventOrigin(new Vector2(eventRect.x, eventRect.center.y), color);
            DrawEventOrigin(new Vector2(eventRect.xMax, eventRect.center.y), color);
        }

        private static void DrawEventOrigin(Vector2 center, Color color)
        {
            Rect originRect = new Rect(center.x - EventOriginSize * 0.5f, center.y - EventOriginSize * 0.5f, EventOriginSize, EventOriginSize);
            EditorGUI.DrawRect(originRect, Color.black);
            Rect innerRect = new Rect(originRect.x + 1f, originRect.y + 1f, originRect.width - 2f, originRect.height - 2f);
            EditorGUI.DrawRect(innerRect, color);
        }

        private static void DrawContextHeader(string label)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUIStyle headerStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    normal = { textColor = new Color(0.5f, 0.5f, 0.5f) },
                };
                GUILayout.Label(label, headerStyle, GUILayout.Width(70f));
                Rect lineRect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(new Rect(lineRect.x, lineRect.y + 7f, lineRect.width, 1f), new Color(0.45f, 0.45f, 0.45f));
                }
            }
        }

        private static float DrawContextFloatField(string label, float value)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            DrawContextLabel(rect, label);
            return EditorGUI.FloatField(GetContextInputRect(rect), value, GetContextInputStyle());
        }

        private static int DrawContextIntField(string label, int value)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            DrawContextLabel(rect, label);
            return EditorGUI.IntField(GetContextInputRect(rect), value, GetContextInputStyle());
        }

        private static void DrawContextLabel(Rect rect, string label)
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = EventContextLabelColor },
            };
            GUI.Label(new Rect(rect.x, rect.y, 132f, rect.height), label, labelStyle);
        }

        private static Rect GetContextInputRect(Rect rect)
        {
            Rect inputRect = new Rect(rect.x + 138f, rect.y + 1f, rect.width - 138f, rect.height - 2f);
            EditorGUI.DrawRect(inputRect, EventContextInputColor);
            DrawBorder(inputRect, new Color(0.02f, 0.02f, 0.02f, 1f));
            return new Rect(inputRect.x + 4f, inputRect.y, inputRect.width - 8f, inputRect.height);
        }

        private static GUIStyle GetContextInputStyle()
        {
            return new GUIStyle(EditorStyles.numberField)
            {
                normal =
                {
                    background = Texture2D.blackTexture,
                    textColor = Color.white,
                },
                focused =
                {
                    background = Texture2D.blackTexture,
                    textColor = Color.white,
                },
                active =
                {
                    background = Texture2D.blackTexture,
                    textColor = Color.white,
                },
            };
        }
    }
}
