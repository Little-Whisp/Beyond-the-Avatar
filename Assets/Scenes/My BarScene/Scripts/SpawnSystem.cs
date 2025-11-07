using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpawnSystem : NetworkBehaviour
{
    public static SpawnSystem Instance { get; private set; }

    [Header("Assign in Inspector")]
    public Transform hostSpawn;                 // Player 1 / Host
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

        // Optional safety: auto-find if list left empty
        if (miniGameSpawns.Count == 0)
        {
            foreach (var sp in FindObjectsOfType<SpawnPoint>())
                miniGameSpawns.Add(sp.transform);
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
        var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
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
            _reserved.Remove(idx);
            _assigned.Remove(clientId);
        }
    }

    public (Vector3 pos, Quaternion rot) ReserveSpawn(ulong clientId)
    {
        // 1) Host / Player 1
        if (NetworkManager.Singleton != null &&
        clientId == NetworkManager.Singleton.LocalClientId &&
        IsServer && hostSpawn != null)
        {
            return (hostSpawn.position, hostSpawn.rotation);
        }
        // 2) Everyone else → mini-game pads
        if (miniGameSpawns.Count == 0)
            return (Vector3.zero, Quaternion.identity); // fallback

        // Already assigned?
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

        if (chosen < 0) chosen = _nextIndex = (_nextIndex + 1) % miniGameSpawns.Count; // reuse if all taken

        _reserved.Add(chosen);
        _assigned[clientId] = chosen;

        var t = miniGameSpawns[chosen];
        return (t.position, t.rotation);
    }
}
