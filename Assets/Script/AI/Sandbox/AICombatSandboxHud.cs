using UnityEngine;

namespace CGame
{
    [RequireComponent(typeof(AICombatSandboxBootstrap))]
    public sealed class AICombatSandboxHud : MonoBehaviour
    {
        private AICombatSandboxBootstrap bootstrap;

        private void Awake()
        {
            bootstrap = GetComponent<AICombatSandboxBootstrap>();
        }

        private void OnGUI()
        {
            if (bootstrap == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(Screen.width - 350f, 16f, 334f, 150f), GUI.skin.box);
            GUILayout.Label("AI COMBAT SANDBOX");
            GUILayout.Label($"Player: {(bootstrap.PlayerReady ? "READY" : "SPAWNING")}");
            GUILayout.Label($"AI: {bootstrap.ReadyAICount}/{bootstrap.RequestedAICount}");
            if (bootstrap.PlayerHealth != null)
            {
                GUILayout.Label($"Player HP: {bootstrap.PlayerHealth.CurrentHealth:F0}/{bootstrap.PlayerHealth.MaxHealth:F0}");
            }

            if (!string.IsNullOrEmpty(bootstrap.FailureMessage))
            {
                GUILayout.Label($"ERROR: {bootstrap.FailureMessage}");
            }
            else
            {
                GUILayout.Label("Click Game view. WASD move, Space jump.");
            }

            GUILayout.EndArea();
        }
    }
}
