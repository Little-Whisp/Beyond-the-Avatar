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

    public(Vector3 pos, Quaternion rot) GetSpawnFor(ulong clientId)
    {
        bool isHost = clientId == NetworkManager.Singleton.LocalClientId
                      && NetworkManager.Singleton.IsHost;

        if (isHost && bartenderSpawn != null)
        {
            Debug.Log("[SpawnSystem] Host bartender spawn");
            return (bartenderSpawn.position, bartenderSpawn.rotation);
        }

        if (!isHost && customerSpawn != null)
        {
            Debug.Log("[SpawnSystem] Client customer spawn");
            return (customerSpawn.position, customerSpawn.rotation);
        }

        return (Vector3.zero, Quaternion.identity);
    }


    public (Vector3 pos, Quaternion rot) GetSpawnForHostState(bool isBartender)
    {
        if (isBartender && bartenderSpawn != null)
            return (bartenderSpawn.position, bartenderSpawn.rotation);
        if (!isBartender && customerSpawn != null)
            return (customerSpawn.position, customerSpawn.rotation);

        // Fallback to miniGameSpawns or zero
        if (miniGameSpawns != null && miniGameSpawns.Count > 0)
            return (miniGameSpawns[0].position, miniGameSpawns[0].rotation);

        return (Vector3.zero, Quaternion.identity);
    }
}
