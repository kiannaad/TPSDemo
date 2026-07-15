using UnityEngine;

namespace CGame
{
    public sealed class AIDecisionDebugPanel : MonoBehaviour
    {
        [SerializeField]
        private AIAlertDecisionRuntimeBehaviour source;

        public AIDecisionDebugSnapshot Snapshot => source?.CreateDebugSnapshot();

        public void Bind(AIAlertDecisionRuntimeBehaviour decisionSource)
        {
            source = decisionSource;
        }

        private void OnGUI()
        {
            AIDecisionDebugSnapshot snapshot = Snapshot;
            if (snapshot == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(16f, 16f, 420f, 520f), GUI.skin.box);
            GUILayout.Label($"Alert: {snapshot.State}");
            GUILayout.Label($"Action: {snapshot.CurrentAction} ({snapshot.ActionStatus})");
            GUILayout.Label($"Reason: {snapshot.SelectionReason}");
            GUILayout.Label($"Commitment: {snapshot.CommitmentRemaining:0.00}s");
            GUILayout.Space(6f);
            GUILayout.Label("Utility candidates");
            AIUtilityCandidate[] candidates = snapshot.Candidates;
            for (int i = 0; i < candidates.Length; i++)
            {
                GUILayout.Label($"  {candidates[i].Kind}: {candidates[i].Score:0.000} ({candidates[i].Reason})");
            }

            GUILayout.Space(6f);
            GUILayout.Label("Alert history");
            AIAlertTransitionRecord[] history = snapshot.History;
            for (int i = 0; i < history.Length; i++)
            {
                GUILayout.Label($"  {history[i].Previous} -> {history[i].Current}: {history[i].Reason}");
            }

            GUILayout.EndArea();
        }
    }
}
