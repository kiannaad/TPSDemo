using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CGame
{
    public sealed class WeaponModelActionPlayer : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        private PlayableGraph graph;
        private AnimationClipPlayable clipPlayable;
        private AnimationClip currentClip;
        private ulong actionId;

        public event Action<ulong, WeaponPresentationEndReason> PresentationEnded;

        public ulong ActionId => actionId;
        public bool IsPlaying => actionId > 0ul && graph.IsValid();

        public bool Play(AnimationClip clip, ulong actionId)
        {
            if (clip == null || animator == null || actionId == 0ul)
            {
                return false;
            }

            if (IsPlaying)
            {
                Finish(WeaponPresentationEndReason.Interrupted);
            }

            EnsureGraph(clip);
            this.actionId = actionId;
            clipPlayable.SetTime(0d);
            clipPlayable.SetDone(false);
            clipPlayable.SetSpeed(1d);
            graph.Play();
            return true;
        }

        public bool Stop(ulong expectedActionId)
        {
            if (!IsPlaying || actionId != expectedActionId)
            {
                return false;
            }

            Finish(WeaponPresentationEndReason.Interrupted);
            return true;
        }

        private void Update()
        {
            if (IsPlaying && clipPlayable.GetTime() >= currentClip.length)
            {
                Finish(WeaponPresentationEndReason.NaturalEnd);
            }
        }

        private void OnDestroy()
        {
            if (IsPlaying)
            {
                Finish(WeaponPresentationEndReason.OwnerDisposed);
            }

            if (graph.IsValid())
            {
                graph.Destroy();
            }
        }

        private void EnsureGraph(AnimationClip clip)
        {
            if (graph.IsValid() && currentClip == clip)
            {
                return;
            }

            if (graph.IsValid())
            {
                graph.Destroy();
            }

            currentClip = clip;
            graph = PlayableGraph.Create($"WeaponModelAction:{name}");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            clipPlayable = AnimationClipPlayable.Create(graph, clip);
            clipPlayable.SetDuration(clip.length);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "WeaponModel", animator);
            output.SetSourcePlayable(clipPlayable);
        }

        private void Finish(WeaponPresentationEndReason reason)
        {
            ulong endedActionId = actionId;
            actionId = 0ul;
            if (clipPlayable.IsValid())
            {
                clipPlayable.SetSpeed(0d);
                clipPlayable.SetTime(0d);
            }

            if (endedActionId > 0ul)
            {
                PresentationEnded?.Invoke(endedActionId, reason);
            }
        }
    }
}
