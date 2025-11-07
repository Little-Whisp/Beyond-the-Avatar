using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

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
        if (ss == null) { Debug.LogWarning("[PlayerSpawnController] No SpawnSystem.Instance found."); return; }

        var (pos, rot) = ss.GetSpawnFor(OwnerClientId);

        bool isHostOwner = IsServer && OwnerClientId == NetworkManager.Singleton.LocalClientId;
        bool lockMovement = ss.lockNonHostAtSpawn && !isHostOwner;

        transform.SetPositionAndRotation(pos, rot);

        ApplySpawnClientRpc(pos, rot, lockMovement, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        });
    }

    [ClientRpc]
    public void ApplySpawnClientRpc(Vector3 pos, Quaternion rot, bool lockMovement, ClientRpcParams rpcParams = default)
    {
        // Ensure we move the *local* XR rig (which is usually NOT a child of the network avatar)
        if (_origin == null)
            _origin = FindObjectOfType<XROrigin>(true); // find the local rig in the scene

        if (_origin != null)
        {
            // XR-safe placement: fixes HMD offset issues
            _origin.MoveCameraToWorldLocation(pos);
            _origin.MatchOriginUpCameraForward(Vector3.up, rot * Vector3.forward);
        }
        else
        {
            // Fallback: move this object if no XR Origin found
            transform.SetPositionAndRotation(pos, rot);
        }

        // SetMovementLocked(lockMovement);
    }


    // public void SetMovementLocked(bool locked)
    // {
    //     // Existing behaviour on the network avatar
    //     if (_teleports != null)
    //         foreach (var t in _teleports) if (t) t.enabled = !locked;

    //     if (_moveProviders != null)
    //         foreach (var m in _moveProviders) if (m) m.enabled = !locked;

    //     var snapTurns = GetComponentsInChildren<ActionBasedSnapTurnProvider>(true);
    //     foreach (var s in snapTurns) if (s) s.enabled = !locked;

    //     // Also try on the local XR Origin (common in XR projects)
    //     var sceneOrigin = _origin != null ? _origin : FindObjectOfType<XROrigin>(true);
    //     if (sceneOrigin != null)
    //     {
    //         foreach (var t in sceneOrigin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>(true))
    //             if (t) t.enabled = !locked;

    //         foreach (var m in sceneOrigin.GetComponentsInChildren<ActionBasedContinuousMoveProvider>(true))
    //             if (m) m.enabled = !locked;

    //         foreach (var s in sceneOrigin.GetComponentsInChildren<ActionBasedSnapTurnProvider>(true))
    //             if (s) s.enabled = !locked;
    //     }
    // }
}
