using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpawnSystem : MonoBehaviour
{
    public static SpawnSystem Instance { get; private set; }

    [Header("Assign in Inspector")]
    public Transform bartenderSpawn;   // PlayerNumber == 1
    public Transform customerSpawn;    // PlayerNumber >= 2
    public List<Transform> miniGameSpawns = new();

    public bool useRandom = false;

    [Header("Movement")]
    public bool lockNonBartenderAtSpawn = false;

    private readonly Dictionary<ulong, int> _assigned = new();
    private readonly HashSet<int> _reserved = new();
    private int _nextIndex;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public (Vector3 pos, Quaternion rot) GetSpawnFor(ulong clientId)
    {
        XRMultiplayer.XRINetworkPlayer xrPlayer = null;

        // Find XRINetworkPlayer by iterating all spawned objects
        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            var playerComp = netObj.GetComponent<XRMultiplayer.XRINetworkPlayer>();
            if (playerComp != null && playerComp.OwnerClientId == clientId)
            {
                xrPlayer = playerComp;
                break;
            }
        }

        if (xrPlayer == null)
        {
            Debug.LogWarning($"[SpawnSystem] No XRINetworkPlayer found for ClientId={clientId}. Fallback to customer spawn.");
            if (customerSpawn != null)
                return (customerSpawn.position, customerSpawn.rotation);
            return (Vector3.zero, Quaternion.identity);
        }

        int number = xrPlayer.PlayerNumber.Value;

        // Bartender (PlayerNumber == 1)
        if (number == 1 && bartenderSpawn != null)
        {
            Debug.Log($"[SpawnSystem] Bartender spawn applied for ClientId={clientId}");
            return (bartenderSpawn.position, bartenderSpawn.rotation);
        }

        // Customers (PlayerNumber >= 2)
        if (number >= 2 && customerSpawn != null)
        {
            Debug.Log($"[SpawnSystem] Customer spawn applied for ClientId={clientId}");
            return (customerSpawn.position, customerSpawn.rotation);
        }

        // Fallback to miniGameSpawns
        if (miniGameSpawns == null || miniGameSpawns.Count == 0)
        {
            Debug.LogWarning("[SpawnSystem] miniGameSpawns empty — fallback to customer spawn");
            if (customerSpawn != null)
                return (customerSpawn.position, customerSpawn.rotation);
            return (Vector3.zero, Quaternion.identity);
        }

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
                if (!_reserved.Contains(r))
                {
                    chosen = r;
                    break;
                }
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
