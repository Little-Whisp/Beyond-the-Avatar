using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class NGOProbe : MonoBehaviour
{
    private IEnumerator Start()
    {
        // Wait until the singleton exists (in case of async instantiation)
        yield return new WaitUntil(() => NetworkManager.Singleton != null);

        // Optional: also wait until it’s running, if you need that
        // yield return new WaitUntil(() => NetworkManager.Singleton.IsListening);

        Debug.Log("[Probe] NetworkManager ready.");
    }
}