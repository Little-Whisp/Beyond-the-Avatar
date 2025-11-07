using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpawnSystem : MonoBehaviour
{
    public static SpawnSystem Instance { get; private set; }

    [Header("Assign in Inspector")]
    public Transform hostSpawn;                 // Host spawn (bar)
    public Transform nonHostSpawn;              // One point in front of the minigame
    public List<Transform> miniGameSpawns = new();

    public bool useRandom = false;

    [Header("Movement")]
    public bool lockNonHostAtSpawn = false;

    private readonly Dictionary<ulong, int> _assigned = new();
    private readonly HashSet<int> _reserved = new();
    private int _nextIndex;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

  private void OnClientConnected(ulong clientId)
{
    // This must be server-only. No RPCs from here.
    if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        return;

    // Reserve a spawn index so PlayerSpawnController gets a stable spot.
    GetSpawnFor(clientId);
}


    private void OnClientDisconnected(ulong clientId)
    {
        if (_assigned.TryGetValue(clientId, out var idx))
        {
            _reserved.Remove(idx);
            _assigned.Remove(clientId);
        }
    }

    // >>> Make this public so PlayerSpawnController can call it too if needed
    public (Vector3 pos, Quaternion rot) GetSpawnFor(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        bool isServer = nm != null && nm.IsServer;

        // Host -> bar
        if (nm != null && clientId == nm.LocalClientId && isServer && hostSpawn != null)
            return (hostSpawn.position, hostSpawn.rotation);

        // Non-host -> single point (if assigned)
        if (nonHostSpawn != null)
            return (nonHostSpawn.position, nonHostSpawn.rotation);

        // Otherwise, optional list logic
        if (miniGameSpawns == null || miniGameSpawns.Count == 0)
            return (Vector3.zero, Quaternion.identity);

        if (_assigned.TryGetValue(clientId, out var existing))
        {
            var t0 = miniGameSpawns[Mathf.Clamp(existing, 0, miniGameSpawns.Count - 1)];
            return (t0.position, t0.rotation);
        }

        int chosen = -1;

        if (useRandom)
        {
            for (int tries = 0; tries < 8; tries++)
            {
                int r = Random.Range(0, miniGameSpawns.Count);
                if (!_reserved.Contains(r)) { chosen = r; break; }
            }
        }

        if (chosen < 0)
        {
            for (int i = 0; i < miniGameSpawns.Count; i++)
            {
                int idx = (_nextIndex + i) % miniGameSpawns.Count;
                if (!_reserved.Contains(idx))
                {
                    chosen = idx;
                    _nextIndex = (idx + 1) % miniGameSpawns.Count;
                    break;
                }
            }
        }

        if (chosen < 0)
            chosen = _nextIndex = (_nextIndex + 1) % miniGameSpawns.Count;

        _reserved.Add(chosen);
        _assigned[clientId] = chosen;

        var t = miniGameSpawns[chosen];
        return (t.position, t.rotation);
    }
}
