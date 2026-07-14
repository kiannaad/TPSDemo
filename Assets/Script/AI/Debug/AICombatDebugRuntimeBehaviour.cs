using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CGame
{
    public sealed class AICombatDebugRuntimeBehaviour : MonoBehaviour
    {
        private AIRuntimeRegistration runtimeRegistration;
        private bool panelVisible;

        public bool PanelVisible => panelVisible;

        public void Initialize(AIRuntimeRegistration registration)
        {
            if (runtimeRegistration != null)
            {
                throw new InvalidOperationException("AI debug runtime is already initialized.");
            }

            runtimeRegistration = registration ?? throw new ArgumentNullException(nameof(registration));
        }

        public void SetPanelVisible(bool visible)
        {
            panelVisible = visible;
        }

        public AICombatDebugSnapshot CreateDebugSnapshot()
        {
            if (runtimeRegistration == null)
            {
                return null;
            }

            double timestamp = Time.timeAsDouble;
            return new AICombatDebugSnapshot(
                runtimeRegistration.RuntimeId.Value,
                runtimeRegistration.IsAlive,
                runtimeRegistration.Perception?.CreateDebugSnapshot(),
                runtimeRegistration.Navigation?.CreateDebugSnapshot(),
                runtimeRegistration.Decision?.CreateDebugSnapshot(),
                runtimeRegistration.CoverCombat?.CreateDebugSnapshot(),
                runtimeRegistration.SquadMember?.CreateDebugSnapshot(timestamp));
        }

        public void Shutdown()
        {
            runtimeRegistration = null;
            panelVisible = false;
            enabled = false;
        }

        private void OnGUI()
        {
            if (!panelVisible)
            {
                return;
            }

            AICombatDebugSnapshot snapshot = CreateDebugSnapshot();
            if (snapshot == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(16f, 16f, 720f, 520f), GUI.skin.box);
            GUILayout.Label($"AI {ShortId(snapshot.RuntimeId)} | Alive: {snapshot.IsAlive}");
            if (snapshot.Decision != null)
            {
                GUILayout.Label($"State: {snapshot.Decision.State} | Action: {snapshot.Decision.CurrentAction}");
                GUILayout.Label(
                    $"Utility: {snapshot.Decision.SelectionReason} | Commitment/cooldown: " +
                    $"{snapshot.Decision.CommitmentRemaining:F2}s");
                AIUtilityCandidate[] candidates = snapshot.Decision.Candidates;
                for (int i = 0; i < Mathf.Min(3, candidates.Length); i++)
                {
                    GUILayout.Label($"  Candidate {candidates[i].Kind}: {candidates[i].Score:F2} ({candidates[i].Reason})");
                }

                AIAlertTransitionRecord[] history = snapshot.Decision.History;
                for (int i = Mathf.Max(0, history.Length - 3); i < history.Length; i++)
                {
                    GUILayout.Label(
                        $"  History {history[i].Previous} -> {history[i].Current}: {history[i].Reason}");
                }
            }

            if (snapshot.Navigation != null)
            {
                GUILayout.Label($"Path: {snapshot.Navigation.PathStatus} / {snapshot.Navigation.FollowState}");
            }

            if (snapshot.CoverCombat != null)
            {
                GUILayout.Label($"Combat: {snapshot.CoverCombat.Action} | Cover: {snapshot.CoverCombat.ReservedSlotId}");
                GUILayout.Label($"Combat reason: {snapshot.CoverCombat.Reason}");
                CoverCandidateScore[] candidates = snapshot.CoverCombat.Candidates;
                for (int i = 0; i < Mathf.Min(3, candidates.Length); i++)
                {
                    GUILayout.Label(
                        $"  Cover {candidates[i].SlotId}: {candidates[i].Score:F2} | viable={candidates[i].IsViable}");
                }
            }

            if (snapshot.Perception != null)
            {
                GUILayout.Label($"Memory: {snapshot.Perception.Records.Length} | Pending: {snapshot.Perception.PendingStimulusCount}");
            }

            if (snapshot.Squad != null)
            {
                GUILayout.Label($"Squad reports: {snapshot.Squad.Reports.Length} | Leases: {snapshot.Squad.Leases.Length}");
                AISquadReport[] reports = snapshot.Squad.Reports;
                for (int i = 0; i < Mathf.Min(2, reports.Length); i++)
                {
                    GUILayout.Label(
                        $"  Intel {ShortId(reports[i].ReporterId)} -> {reports[i].SubjectId}: " +
                        $"confidence={reports[i].Confidence:F2}, uncertainty={reports[i].UncertaintyRadius:F1}m");
                }

                AISquadLeaseDebugRecord[] leases = snapshot.Squad.Leases;
                for (int i = 0; i < Mathf.Min(3, leases.Length); i++)
                {
                    GUILayout.Label(
                        $"  Lease {leases[i].Kind}/{leases[i].ResourceId}: " +
                        $"owner={ShortId(leases[i].OwnerId)}, remaining={Math.Max(0d, leases[i].ExpiresAt - snapshot.Squad.CapturedAt):F2}s");
                }
            }

            GUILayout.Label("Debug is read-only: no Tick, path refresh, selection, or stimulus consumption.");
            GUILayout.EndArea();
        }

        private void OnDrawGizmos()
        {
            if (runtimeRegistration == null || runtimeRegistration.Transform == null)
            {
                return;
            }

            DrawPerceptionGizmos();
            DrawNavigationGizmos();
            DrawCoverGizmos();
            DrawSquadGizmos();
        }

        private void DrawPerceptionGizmos()
        {
            Transform observer = runtimeRegistration.Transform;
            float distance = runtimeRegistration.Perception?.ViewDistance ?? 0f;
            float halfAngle = (runtimeRegistration.Perception?.HorizontalFieldOfView ?? 0f) * 0.5f;
            Vector3 origin = observer.position + Vector3.up * 1.6f;
            if (distance > 0f)
            {
                Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.8f);
                Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, -halfAngle, 0f) * observer.forward * distance);
                Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, halfAngle, 0f) * observer.forward * distance);
            }

            AIPerceptionDebugSnapshot perception = runtimeRegistration.Perception?.CreateDebugSnapshot();
            AIPerceptionMemoryRecord[] records = perception?.Records ?? Array.Empty<AIPerceptionMemoryRecord>();
            for (int i = 0; i < records.Length; i++)
            {
                AIPerceptionMemoryRecord record = records[i];
                Gizmos.color = record.IsPrecise ? Color.red : new Color(1f, 0.5f, 0f, 0.8f);
                Gizmos.DrawWireSphere(record.LastKnownPosition, Mathf.Max(0.15f, record.UncertaintyRadius));
                Gizmos.DrawLine(origin, record.LastKnownPosition);
            }
        }

        private void DrawNavigationGizmos()
        {
            AINavigationDebugSnapshot navigation = runtimeRegistration.Navigation?.CreateDebugSnapshot();
            Vector3[] corners = navigation?.Corners ?? Array.Empty<Vector3>();
            Gizmos.color = Color.cyan;
            for (int i = 1; i < corners.Length; i++)
            {
                Gizmos.DrawLine(corners[i - 1] + Vector3.up * 0.1f, corners[i] + Vector3.up * 0.1f);
            }
        }

        private void DrawCoverGizmos()
        {
            AICoverCombatDebugSnapshot combat = runtimeRegistration.CoverCombat?.CreateDebugSnapshot();
            CoverCandidateScore[] candidates = combat?.Candidates ?? Array.Empty<CoverCandidateScore>();
            CoverSlotBehaviour[] slots = CoverSlotBehaviour.CopyActiveSlots();
            for (int i = 0; i < candidates.Length; i++)
            {
                for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
                {
                    if (!string.Equals(candidates[i].SlotId, slots[slotIndex].SlotId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Gizmos.color = candidates[i].IsViable ? Color.green : Color.gray;
                    Vector3 labelPosition = slots[slotIndex].Position + Vector3.up * 0.2f;
                    Gizmos.DrawWireCube(labelPosition, Vector3.one * 0.35f);
#if UNITY_EDITOR
                    Handles.Label(labelPosition + Vector3.up * 0.25f, $"{candidates[i].SlotId} {candidates[i].Score:F2}");
#endif
                    break;
                }
            }
        }

        private void DrawSquadGizmos()
        {
            AISquadDebugSnapshot squad = runtimeRegistration.SquadMember?.CreateDebugSnapshot(Time.timeAsDouble);
            AISquadReport[] reports = squad?.Reports ?? Array.Empty<AISquadReport>();
            Gizmos.color = new Color(0.8f, 0.2f, 1f, 0.8f);
            for (int i = 0; i < reports.Length; i++)
            {
                Gizmos.DrawWireSphere(reports[i].EstimatedPosition, reports[i].UncertaintyRadius);
            }

            AISquadLeaseDebugRecord[] leases = squad?.Leases ?? Array.Empty<AISquadLeaseDebugRecord>();
            CoverSlotBehaviour[] slots = CoverSlotBehaviour.CopyActiveSlots();
            for (int leaseIndex = 0; leaseIndex < leases.Length; leaseIndex++)
            {
                if (leases[leaseIndex].Kind != AISquadResourceKind.Cover)
                {
                    continue;
                }

                for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
                {
                    if (!string.Equals(leases[leaseIndex].ResourceId, slots[slotIndex].SlotId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Vector3 labelPosition = slots[slotIndex].Position + Vector3.up * 0.6f;
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireCube(labelPosition, Vector3.one * 0.5f);
#if UNITY_EDITOR
                    Handles.Label(
                        labelPosition + Vector3.up * 0.3f,
                        $"LEASE {ShortId(leases[leaseIndex].OwnerId)} " +
                        $"{Math.Max(0d, leases[leaseIndex].ExpiresAt - Time.timeAsDouble):F1}s");
#endif
                    break;
                }
            }
        }

        private static string ShortId(string value)
        {
            return string.IsNullOrEmpty(value) || value.Length <= 8 ? value : value.Substring(0, 8);
        }

        private void OnDestroy()
        {
            Shutdown();
        }
    }
}
