using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

public class NetworkAvatarHeight : NetworkBehaviour
{
    [Header("Set player height in meters (eye level)")]
    public float playerHeight = 1.7f;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        // Find XROrigin in the scene
        XROrigin xrOrigin = FindAnyObjectByType<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogWarning("No XROrigin found in scene.");
            return;
        }

        // Find the camera under the XROrigin and use its parent as the height offset
        Camera cam = xrOrigin.GetComponentInChildren<Camera>();
        if (cam == null)
        {
            Debug.LogWarning("No Camera found under XROrigin.");
            return;
        }
        Transform cameraTransform = cam.transform;
        Transform cameraOffset = cameraTransform.parent;

        if (cameraOffset == null)
        {
            Debug.LogWarning("No Camera Offset found.");
            return;
        }

        // Apply height by adjusting Y position
        Vector3 pos = cameraOffset.localPosition;
        pos.y = playerHeight - 0.1f; // adjust eye height slightly
        cameraOffset.localPosition = pos;

        Debug.Log("Applied height: " + pos.y);
    }
}
