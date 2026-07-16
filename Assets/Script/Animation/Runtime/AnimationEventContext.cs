using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGame.Animation
{
    public sealed class AnimationEventContext
    {
        private readonly List<string> contextTags;

        public AnimationEventContext(
            UnityEngine.Object owner,
            AnimationAssetBase animationAsset,
            AnimationNotifyEvent notifyEvent,
            float normalizedTime,
            float deltaTime,
            float weight,
            Transform attachPoint = null,
            string boneOrSocketName = "",
            Vector3? position = null,
            Quaternion? rotation = null)
        {
            Owner = owner;
            OwnerGameObject = ResolveOwnerGameObject(owner);
            OwnerTransform = OwnerGameObject != null ? OwnerGameObject.transform : null;
            Animator = OwnerGameObject != null ? OwnerGameObject.GetComponent<Animator>() : null;
            AnimationAsset = animationAsset;
            NotifyEvent = notifyEvent;
            EventTag = notifyEvent != null && notifyEvent.Notify != null ? notifyEvent.Notify.EventTag : string.Empty;
            contextTags = CopyContextTags(notifyEvent);
            NormalizedTime = normalizedTime;
            DeltaTime = Math.Max(0f, deltaTime);
            Weight = Math.Max(0f, weight);
            AttachPoint = attachPoint;
            BoneOrSocketName = string.IsNullOrWhiteSpace(boneOrSocketName) ? string.Empty : boneOrSocketName.Trim();
            Position = position ?? ResolvePosition(attachPoint, OwnerTransform);
            Rotation = rotation ?? ResolveRotation(attachPoint, OwnerTransform);
        }

        public UnityEngine.Object Owner { get; }
        public GameObject OwnerGameObject { get; }
        public Transform OwnerTransform { get; }
        public Animator Animator { get; }
        public AnimationAssetBase AnimationAsset { get; }
        public AnimationNotifyEvent NotifyEvent { get; }
        public string EventTag { get; }
        public IReadOnlyList<string> ContextTags => contextTags;
        public float NormalizedTime { get; }
        public float DeltaTime { get; }
        public float Weight { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Transform AttachPoint { get; }
        public string BoneOrSocketName { get; }

        private static GameObject ResolveOwnerGameObject(UnityEngine.Object owner)
        {
            if (owner is GameObject gameObject)
            {
                return gameObject;
            }

            if (owner is Component component)
            {
                return component.gameObject;
            }

            return null;
        }

        private static Vector3 ResolvePosition(Transform attachPoint, Transform ownerTransform)
        {
            if (attachPoint != null)
            {
                return attachPoint.position;
            }

            return ownerTransform != null ? ownerTransform.position : Vector3.zero;
        }

        private static Quaternion ResolveRotation(Transform attachPoint, Transform ownerTransform)
        {
            if (attachPoint != null)
            {
                return attachPoint.rotation;
            }

            return ownerTransform != null ? ownerTransform.rotation : Quaternion.identity;
        }

        private static List<string> CopyContextTags(AnimationNotifyEvent notifyEvent)
        {
            var tags = new List<string>();
            if (notifyEvent == null || notifyEvent.Notify == null)
            {
                return tags;
            }

            foreach (string tag in notifyEvent.Notify.ContextTags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    tags.Add(tag.Trim());
                }
            }

            return tags;
        }
    }
}
