using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpawnSystem : NetworkBehaviour
{
    public static SpawnSystem Instance { get; private set; }

    [Header("Assign in Inspector")]
    public Transform hostSpawn;                    // Player 1 / Host
    public List<Transform> miniGameSpawns = new(); // Players 2,3,4...

    [Tooltip("If true, non-host spawns are random; otherwise round-robin.")]
    public bool useRandom = false;

    private readonly Dictionary<ulong, int> _assigned = new();
    private readonly HashSet<int> _reserved = new();
    private int _nextIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Optional safety: auto-find if list left empty (uses tag instead of a custom type)
        // Tag your client spawn pads with: "ClientSpawn"
        if (miniGameSpawns.Count == 0)
        {
            var tagged = GameObject.FindGameObjectsWithTag("ClientSpawn");
            foreach (var go in tagged)
            {
                if (go != null) miniGameSpawns.Add(go.transform);
            }
        }

        // Ensure host spawn is NOT in the client pool
        if (hostSpawn != null)
        {
            miniGameSpawns.RemoveAll(t => t == null || t == hostSpawn);
        }

        if (miniGameSpawns.Count == 0)
        {
            Debug.LogError("[SpawnSystem] No mini-game spawn points found. Assign in Inspector or tag pads as 'ClientSpawn'.", this);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        NetworkManager.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        NetworkManager.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        var (pos, rot) = ReserveSpawn(clientId);

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            return;

        var playerObj = client.PlayerObject;
        if (playerObj != null && playerObj.TryGetComponent<PlayerSpawnController>(out var psc))
        {
            psc.ApplySpawnClientRpc(pos, rot, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            });
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (_assigned.TryGetValue(clientId, out var idx))
        {
            if (idx >= 0) _reserved.Remove(idx); // ignore sentinel host value
            _assigned.Remove(clientId);
        }
    }

    public (Vector3 pos, Quaternion rot) ReserveSpawn(ulong clientId)
    {
        // 1) Host / Server gets the dedicated hostSpawn
        if (NetworkManager.Singleton != null &&
            IsServer &&
            clientId == NetworkManager.ServerClientId &&
            hostSpawn != null)
        {
            _assigned[clientId] = -999; // sentinel marking host
            return (hostSpawn.position, hostSpawn.rotation);
        }

        // 2) Everyone else → mini-game pads
        if (miniGameSpawns.Count == 0)
            return (Vector3.zero, Quaternion.identity); // fallback

        // Already assigned?
        if (_assigned.TryGetValue(clientId, out var existing))
        {
            if (existing >= 0)
            {
                var t0 = miniGameSpawns[Mathf.Clamp(existing, 0, miniGameSpawns.Count - 1)];
                return (t0.position, t0.rotation);
            }
            // If it was -999 (host sentinel) but somehow we’re here, just use first client pad
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
        {
            // All taken → reuse round-robin
            chosen = _nextIndex = (_nextIndex + 1) % miniGameSpawns.Count;
        }

        _reserved.Add(chosen);
        _assigned[clientId] = chosen;

        var t = miniGameSpawns[chosen];
        return (t.position, t.rotation);
    }
}
