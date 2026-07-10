using System;
using UnityEngine;

namespace CGame.Animation
{
    [Serializable]
    public abstract class AnimationNotify
    {
        [SerializeField] private string displayName = "Notify";

        public string DisplayName
        {
            get => displayName;
            set => displayName = string.IsNullOrWhiteSpace(value) ? "Notify" : value;
        }
    }
}
