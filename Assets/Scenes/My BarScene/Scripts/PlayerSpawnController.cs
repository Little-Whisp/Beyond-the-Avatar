using Unity.Netcode;
using UnityEngine;
using Unity.XR.CoreUtils;

public class PlayerSpawnController : NetworkBehaviour
{
    private XROrigin _xr;

    private void Awake()
    {
        _xr = GetComponentInChildren<XROrigin>(true);
        if (_xr == null) Debug.LogWarning("XROrigin not found under player prefab.");
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner || !IsClient) return;

        // Ask server for our spawn immediately after spawn
        if (IsServer)
        {
            // Host: call directly
            var (pos, rot) = SpawnSystem.Instance ? SpawnSystem.Instance.ReserveSpawn(OwnerClientId) : (Vector3.zero, Quaternion.identity);
            ApplySpawnClientRpc(pos, rot, default);
        }
        else
        {
            RequestSpawnServerRpc();
        }
    }

    [ServerRpc]
    private void RequestSpawnServerRpc(ServerRpcParams _ = default)
    {
        var (pos, rot) = SpawnSystem.Instance ? SpawnSystem.Instance.ReserveSpawn(OwnerClientId) : (Vector3.zero, Quaternion.identity);
        ApplySpawnClientRpc(pos, rot, new ClientRpcParams {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        });
    }

    [ClientRpc]
    public void ApplySpawnClientRpc(Vector3 pos, Quaternion rot, ClientRpcParams _ = default)
    {
        if (!IsOwner) return;

        if (_xr != null)
        {
            // Align rig up/forward, then place the HMD at spawn position.
            _xr.MatchOriginUpCameraForward(Vector3.up, rot * Vector3.forward);
            _xr.MoveCameraToWorldLocation(pos);
        }
        else
        {
            // Fallback
            transform.SetPositionAndRotation(pos, rot);
        }
    }
}
