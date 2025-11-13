using Unity.Netcode;
using UnityEngine;

public class UnlockLocomotionOnJoin : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        // Only affect the local player who owns this avatar
        if (!IsOwner) return;

        var locker = FindObjectOfType<LocomotionLocker>(true);
        if (locker != null)
            locker.Unlock();
        else
            Debug.LogWarning("UnlockLocomotionOnJoin: No LocomotionLocker found in scene.");
    }
}
