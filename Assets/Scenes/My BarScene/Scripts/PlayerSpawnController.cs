using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using XRMultiplayer; // <-- important so we can access XRINetworkPlayer

public class PlayerSpawnController : NetworkBehaviour
{
    XROrigin _origin;
    UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider[] _teleports;
    ActionBasedContinuousMoveProvider[] _moveProviders;

    void Awake()
    {
        _origin = GetComponentInChildren<XROrigin>(true);
        _teleports = GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>(true);
        _moveProviders = GetComponentsInChildren<ActionBasedContinuousMoveProvider>(true);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        var ss = SpawnSystem.Instance;
        if (ss == null)
        {
            Debug.LogWarning("[PlayerSpawnController] No SpawnSystem.Instance found.");
            return;
        }

        var (pos, rot) = ss.GetSpawnFor(OwnerClientId);

        // Get XRINetworkPlayer to check PlayerNumber
        var playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(OwnerClientId);
        var xrPlayer = playerObj != null ? playerObj.GetComponent<XRINetworkPlayer>() : null;

        bool isBartender = xrPlayer != null && xrPlayer.PlayerNumber.Value == 1;

        // Only bartender gets movement if lock enabled
        bool lockMovement = ss.lockNonBartenderAtSpawn && !isBartender;

        transform.SetPositionAndRotation(pos, rot);

        ApplySpawnClientRpc(pos, rot, lockMovement, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        });
    }

    [ClientRpc]
    public void ApplySpawnClientRpc(Vector3 pos, Quaternion rot, bool lockMovement, ClientRpcParams rpcParams = default)
    {
        if (_origin == null)
            _origin = FindObjectOfType<XROrigin>(true);

        if (_origin != null)
        {
            _origin.MoveCameraToWorldLocation(pos);
            _origin.MatchOriginUpCameraForward(Vector3.up, rot * Vector3.forward);
        }
        else
        {
            transform.SetPositionAndRotation(pos, rot);
        }

        if (_teleports == null || _teleports.Length == 0)
            _teleports = GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>(true);

        if (_moveProviders == null || _moveProviders.Length == 0)
            _moveProviders = GetComponentsInChildren<ActionBasedContinuousMoveProvider>(true);

        foreach (var tp in _teleports)
            if (tp != null) tp.enabled = !lockMovement;

        foreach (var mp in _moveProviders)
            if (mp != null) mp.enabled = !lockMovement;
    }
}
