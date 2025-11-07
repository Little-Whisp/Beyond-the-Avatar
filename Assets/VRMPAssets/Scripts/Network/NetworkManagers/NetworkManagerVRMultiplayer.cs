using System;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XRMultiplayer
{
    public class NetworkManagerVRMultiplayer : NetworkManager
    {
        [Header("Runtime Controls")]
        [SerializeField] LogLevel m_LogLevel = LogLevel.Developer;
        [SerializeField] bool m_RunInBackground = true;

        [SerializeField] NetworkConfig m_NetworkConfig;

        [SerializeField, Tooltip("If true, require/expect an approval callback.")]
        bool m_UseConnectionApproval = true;

        [SerializeField, Tooltip("Desired max clients (if the runtime property exists).")]
        int m_MaxClients = 4;

        void Awake()
        {
            LogLevel = m_LogLevel;
            RunInBackground = m_RunInBackground;

            if (m_NetworkConfig == null)
                m_NetworkConfig = new NetworkConfig();

            // Apply serialized config to the live manager
            NetworkConfig = m_NetworkConfig;

            // ---- Version-proof toggles (reflection so older/newer NGO won't break) ----
            TrySetProperty(NetworkConfig, "ConnectionApproval", m_UseConnectionApproval);
            TrySetProperty(this, "MaxConnectedClients", (ulong)Mathf.Max(1, m_MaxClients));

            // If approval is enabled in your config, wire a safe allow-all callback
            ConnectionApprovalCallback = OnConnectionApproval;

            OnClientConnectedCallback += id =>
                Debug.Log($"[NGO] Client {id} approved & connected.");

            OnClientDisconnectCallback += id =>
            {
                var reason = string.IsNullOrEmpty(DisconnectReason) ? "(no reason)" : DisconnectReason;
                Debug.LogWarning($"[NGO] Client {id} disconnected. Reason: {reason}");
            };

            // Basic prefab guard
            if (NetworkConfig.PlayerPrefab == null)
            {
                Debug.LogError("[NGO] Player Prefab is NOT set. Assign one with a NetworkObject.");
            }
            else if (NetworkConfig.PlayerPrefab.GetComponent<NetworkObject>() == null)
            {
                Debug.LogError("[NGO] Player Prefab is missing a NetworkObject component.");
            }

            Utils.s_LogLevel = LogLevel;
        }

        // Simple allow-all approval (runs only if your config actually requires approval)
        private void OnConnectionApproval(NetworkManager.ConnectionApprovalRequest req,
                                          NetworkManager.ConnectionApprovalResponse res)
        {
            try
            {
                res.Approved = true;
                res.CreatePlayerObject = true;
                res.Position = Vector3.zero;
                res.Rotation = Quaternion.identity;
                res.Pending = false;
                res.Reason = string.Empty;
            }
            catch (Exception ex)
            {
                res.Approved = false;
                res.CreatePlayerObject = false;
                res.Pending = false;
                res.Reason = "Approval exception: " + ex.Message;
                Debug.LogException(ex);
            }
        }

        // -------- helpers --------
        static void TrySetProperty(object target, string propertyName, object value)
        {
            if (target == null) return;
            var p = target.GetType().GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanWrite)
            {
                try { p.SetValue(target, value); }
                catch (Exception e) { Debug.Log($"[NGO] Could not set {propertyName}: {e.Message}"); }
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(NetworkManagerVRMultiplayer))]
    class VRMutliplayerTemplateNetworkManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying)
            {
                switch (XRINetworkGameManager.CurrentConnectionState.Value)
                {
                    case XRINetworkGameManager.ConnectionState.None:
                    case XRINetworkGameManager.ConnectionState.Authenticating:
                        GUILayout.Box("Authenticating"); break;
                    case XRINetworkGameManager.ConnectionState.Authenticated:
                        if (GUILayout.Button("Connect")) XRINetworkGameManager.Instance.QuickJoinLobby();
                        break;
                    case XRINetworkGameManager.ConnectionState.Connecting:
                        GUILayout.Box("Connecting"); break;
                    case XRINetworkGameManager.ConnectionState.Connected:
                        if (GUILayout.Button("Disconnect")) XRINetworkGameManager.Instance.Disconnect();
                        break;
                }
            }
            else
            {
                GUILayout.Box("Game not running.");
            }
        }
    }
#endif
}
