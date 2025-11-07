using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class NGOBootstrap : MonoBehaviour
{
    [SerializeField] private Unity.Netcode.NetworkManager networkManagerPrefab;

    private void Awake()
    {
        if (Unity.Netcode.NetworkManager.Singleton == null)
        {
            Instantiate(networkManagerPrefab);
            Debug.Log("[NGOBootstrap] Instantiated NetworkManager prefab.");
        }
    }
}
