using UnityEngine;
using Unity.Netcode;

public class LocomotionLocker : MonoBehaviour
{
    [Tooltip("Parent object that contains Turn, Move, Teleportation, etc.")]
    public GameObject locomotionRoot;

    [Header("Auto-lock rules")]
    [Tooltip("If true, locomotion is disabled when the scene starts (lobby).")]
    public bool lockedAtStart = true;

    [Tooltip("If true, this will ALSO lock locomotion for the HOST (bartender) automatically.")]
    public bool lockForHost = true;

    [Tooltip("Optional: place host at this spawn when locking (bar position).")]
    public Transform hostSpawn;

    bool _applied;

    void Start()
    {
        // Lobby behavior still works
        if (lockedAtStart)
            SetLocked(true);
    }

    void Update()
    {
        if (_applied) return;

        if (!lockForHost) return;
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsListening) return;

        // Only the local player should lock their own locomotion
        // If this exists in the player prefab, it will run per-player.
        var netObj = GetComponentInParent<NetworkObject>();
        if (netObj != null && !netObj.IsOwner) return;

        bool isHost = NetworkManager.Singleton.IsServer;
        if (!isHost) { _applied = true; return; }

        // Lock host locomotion
        SetLocked(true);

        // Optional: snap host to bar spawn
        if (hostSpawn != null)
            transform.root.SetPositionAndRotation(hostSpawn.position, hostSpawn.rotation);

        _applied = true;
    }

    public void SetLocked(bool locked)
    {
        if (locomotionRoot != null)
            locomotionRoot.SetActive(!locked);
    }

    public void Lock() => SetLocked(true);
    public void Unlock() => SetLocked(false);
}
