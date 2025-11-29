using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.XR.CoreUtils;

[DisallowMultipleComponent]
public class MiniGameLocalMovementBinder : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public BoxCollider lobbyArea;       // trigger box for lobby
    public BoxCollider miniGameArea;    // trigger box for minigame

    [Header("Behavior")]
    [Tooltip("Also clamp the host? (OFF = clients only)")]
    public bool affectHost = false;

    [Tooltip("Clamp Y too? Usually keep OFF so height isn't forced.")]
    public bool clampY = false;

    XROrigin _origin;
    MiniGameJoinArea _limiter;

    bool _initialized;
    bool _shouldAffect;

    enum ClampMode { Off, Lobby, MiniGame }
    ClampMode _mode = ClampMode.Lobby; // default: clamp in lobby

    void Start()
    {
        // safety: make areas triggers so they never push the capsule
        if (lobbyArea) lobbyArea.isTrigger = true;
        if (miniGameArea) miniGameArea.isTrigger = true;

        StartCoroutine(InitWhenReady());
    }

    IEnumerator InitWhenReady()
    {
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

        // dedicated server: no clamp
        if (nm.IsServer && !nm.IsClient)
        {
            _initialized = true;
            _shouldAffect = false;
            return;
        }

        bool isClientOnly = nm.IsClient && !nm.IsServer;
        bool isHost = nm.IsClient && nm.IsServer;

        _shouldAffect = isClientOnly || (isHost && affectHost);
        _initialized = true;

        // Bartender check BEFORE any clamp logic
        var xrPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject()
                    .GetComponent<XRMultiplayer.XRINetworkPlayer>();

        if (xrPlayer != null && xrPlayer.PlayerNumber.Value == 1)
        {
            // Bartender, do NOT clamp
            _shouldAffect = false;
            Debug.Log("[Binder] Bartender detected — clamp disabled.");
            return;
        }

        if (!_shouldAffect) return;
        if (!BindRig()) return;

        ApplyClamp(); // start in lobby
    }

    void Update()
    {
        if (!_initialized || !_shouldAffect) return;
        if (!BindRig()) return;

        // keep limits synced if boxes move/scale
        ApplyClamp(updateOnly: true);
    }

    void ApplyClamp(bool updateOnly = false)
    {
        if (_limiter == null) return;

        BoxCollider area = null;
        switch (_mode)
        {
            case ClampMode.Lobby: area = lobbyArea; break;
            case ClampMode.MiniGame: area = miniGameArea; break;
            case ClampMode.Off: area = null; break;
        }

        if (area == null)
        {
            _limiter.enabled = false;
            return;
        }

        // XZ clamp by default; keep Y free unless asked
        Vector3 center = area.bounds.center;
        Vector3 size = area.bounds.size;

        if (!clampY)
        {
            if (_origin == null) BindRig();
            float y = _origin ? _origin.transform.position.y : center.y;
            center.y = y;
            size.y = 1000f; // effectively no Y clamp
        }

        _limiter.SetArea(center, size);
        if (!updateOnly) _limiter.enabled = true;
    }

    bool BindRig()
    {
        if (_origin == null)
        {
#if UNITY_2023_1_OR_NEWER
            _origin = Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
#else
            _origin = Object.FindObjectOfType<XROrigin>(true);
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

    // ---- Manual controls you can call from your flow/UI ----
    [ContextMenu("Enable Lobby Clamp")]
    public void EnableLobbyClamp()
    {
        _mode = ClampMode.Lobby;
        if (_shouldAffect && BindRig()) ApplyClamp(updateOnly: false);
    }

    [ContextMenu("Enable MiniGame Clamp")]
    public void EnableMiniGameClamp()
    {
        _mode = ClampMode.MiniGame;
        if (_shouldAffect && BindRig()) ApplyClamp(updateOnly: false);
    }

    [ContextMenu("Disable Clamp")]
    public void DisableClamp()
    {
        _mode = ClampMode.Off;
        if (_limiter) _limiter.enabled = false;
    }

    void OnDisable()
    {
        if (_limiter) _limiter.enabled = false;
    }
}
