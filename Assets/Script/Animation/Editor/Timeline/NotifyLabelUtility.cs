using System;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace CGame.Animation.Editor
{
    public static class NotifyLabelUtility
    {
        public static string GetLabel(AnimationNotify notify)
        {
            if (notify == null)
            {
                return "Notify";
            }

            Type type = notify.GetType();
            for (Type currentType = type; currentType != null && currentType != typeof(AnimationNotify); currentType = currentType.BaseType)
            {
                FieldInfo field = currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .FirstOrDefault(fieldInfo => fieldInfo.FieldType == typeof(string));
                if (field != null && field.GetValue(notify) is string fieldValue && !string.IsNullOrWhiteSpace(fieldValue))
                {
                    return fieldValue;
                }
            }

            if (!string.IsNullOrWhiteSpace(notify.DisplayName))
            {
                return notify.DisplayName;
            }

            return ObjectNames.NicifyVariableName(type.Name);
        }
    }
}
