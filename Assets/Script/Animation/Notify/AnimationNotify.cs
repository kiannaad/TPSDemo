using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGame.Animation
{
    [Serializable]
    public abstract class AnimationNotify
    {
        [SerializeField] private string displayName = "Notify";
        [SerializeField] private string eventTag = string.Empty;
        [SerializeField] private List<string> contextTags = new List<string>();
        [SerializeField] private AnimationNotifyDispatchPolicy dispatchPolicy = AnimationNotifyDispatchPolicy.DirectNotify;

        public string DisplayName
        {
            get => displayName;
            set => displayName = string.IsNullOrWhiteSpace(value) ? "Notify" : value;
        }

        public string EventTag
        {
            get => eventTag;
            set => eventTag = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        public List<string> ContextTags => contextTags;

        public AnimationNotifyDispatchPolicy DispatchPolicy
        {
            get => dispatchPolicy;
            set => dispatchPolicy = value;
        }

        public void SetContextTags(IEnumerable<string> tags)
        {
            contextTags.Clear();

            if (tags == null)
            {
                return;
            }

            foreach (string tag in tags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    contextTags.Add(tag.Trim());
                }
            }
        }
    }
}
