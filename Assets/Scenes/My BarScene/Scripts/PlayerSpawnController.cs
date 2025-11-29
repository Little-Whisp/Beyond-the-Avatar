using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using XRMultiplayer; // <-- needed to access XRINetworkPlayer

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
        // Only apply spawn logic to the local player
        if (!IsOwner)
            return;

        var xrPlayer = GetComponent<XRINetworkPlayer>();
        if (xrPlayer == null)
            return;

        // Listen for PlayerNumber assignment
        xrPlayer.PlayerNumber.OnValueChanged += OnPlayerNumberAssigned;

        // If already assigned, apply immediately
        if (xrPlayer.PlayerNumber.Value >= 0)
            OnPlayerNumberAssigned(-1, xrPlayer.PlayerNumber.Value);

    }

    private void OnPlayerNumberAssigned(int oldValue, int newValue)
    {
        if (newValue <= 0)
            return;

        Debug.Log($"[PlayerSpawnController] PlayerNumber received: {newValue} for ClientId={OwnerClientId}. Requesting spawn...");

        var ss = SpawnSystem.Instance;
        if (ss == null)
            return;

        var (pos, rot) = ss.GetSpawnFor(OwnerClientId);

        // Bartender = PlayerNumber 1
        bool lockMovement = ss.lockNonBartenderAtSpawn && newValue != 1;

        Debug.Log($"[PlayerSpawnController] Applying spawn for ClientId={OwnerClientId} at pos={pos} rot={rot} lockMovement={lockMovement}");

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
