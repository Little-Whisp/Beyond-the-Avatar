using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.XR.CoreUtils;

[DisallowMultipleComponent]
public class MiniGameLocalMovementBinder : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public BoxCollider restrictedArea;

    [Header("Behavior")]
    [Tooltip("Also clamp the host? (OFF = clients only)")]
    public bool affectHost = false;

    XROrigin _origin;
    MiniGameJoinArea _limiter;

    bool _initialized;
    bool _shouldAffect;

    void Start()
    {
        // Start as clamped (lobby / pre-minigame)
        StartCoroutine(InitWhenReady());
    }

    IEnumerator InitWhenReady()
    {
        // Wait for Netcode + local player so we can decide host/client correctly.
        yield return new WaitUntil(() =>
            NetworkManager.Singleton &&
            (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) &&
            NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null);

        TryInit();
    }

    void TryInit()
    {
        var nm = NetworkManager.Singleton;
        if (!nm) return;

        // Don't run on dedicated server
        if (nm.IsServer && !nm.IsClient)
        {
            _initialized = true;
            _shouldAffect = false;
            return;
        }

        bool isClientOnly = nm.IsClient && !nm.IsServer;
        bool isHost       = nm.IsClient &&  nm.IsServer;

        _shouldAffect = isClientOnly || (isHost && affectHost);
        _initialized  = true;

        if (!_shouldAffect) return;

        if (!restrictedArea)
        {
            Debug.LogWarning("[MiniGameLocalMovementBinder] No restrictedArea assigned, clamping will be inactive.");
            return;
        }

        if (!BindRig()) return;

        // Clamp ON by default
        _limiter.SetArea(restrictedArea.bounds.center, restrictedArea.bounds.size);
        _limiter.enabled = true;
    }

    void Update()
    {
        if (!_initialized || !_shouldAffect) return;
        if (!restrictedArea) return;
        if (!BindRig()) return;

        // Keep bounds fresh if the box moves/scales
        _limiter.SetArea(restrictedArea.bounds.center, restrictedArea.bounds.size);
    }

    bool BindRig()
    {
        if (_origin == null)
        {
#if UNITY_2023_1_OR_NEWER
            _origin = UnityEngine.Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
#else
            _origin = UnityEngine.Object.FindObjectOfType<XROrigin>(true);
#endif
        }
        if (_origin == null) return false;

        if (_limiter == null)
        {
            _limiter = _origin.GetComponent<MiniGameJoinArea>();
            if (_limiter == null)
                _limiter = _origin.gameObject.AddComponent<MiniGameJoinArea>();
        }
        return true;
    }

    // === Public controls you can hook directly from UI buttons ===

    [ContextMenu("Disable Clamp (Join MiniGame)")]
    public void DisableClampForLocalPlayer()
    {
        if (_limiter)
        {
            _limiter.enabled = false;
            Debug.Log("[MiniGameLocalMovementBinder] Clamp disabled for local player (joined mini-game).");
        }
    }

    [ContextMenu("Enable Clamp (Return to Lobby)")]
    public void EnableClampForLocalPlayer()
    {
        if (!BindRig()) return;
        if (!restrictedArea) return;

        _limiter.SetArea(restrictedArea.bounds.center, restrictedArea.bounds.size);
        _limiter.enabled = true;
        Debug.Log("[MiniGameLocalMovementBinder] Clamp enabled for local player (returned to lobby).");
    }
}
