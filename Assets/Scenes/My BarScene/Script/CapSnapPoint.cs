using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor))]
public class CapSnapPoint : MonoBehaviour
{
    public ShakerContainer container;      // shaker body
    public string capTipName = "PourOrigin";  // optional: if you want a different origin when capped

    UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;
    int capLayer;

    void Awake()
    {
        socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        socket.selectEntered.AddListener(OnEnter);
        socket.selectExited .AddListener(OnExit);

        capLayer = LayerMask.NameToLayer("caponly");
        if (capLayer < 0)
            Debug.LogWarning("Layer 'caponly' not found. Put the cap on that layer and set the socket Interaction Layer Mask to caponly.");
    }

    void OnDestroy()
    {
        if (socket)
        {
            socket.selectEntered.RemoveListener(OnEnter);
            socket.selectExited .RemoveListener(OnExit);
        }
    }

    bool IsCap(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRInteractable x)
    {
        var go = (x as Component)?.gameObject;
        return go && (capLayer >= 0) && go.layer == capLayer;
    }

    void OnEnter(SelectEnterEventArgs args)
    {
        if (!IsCap(args.interactableObject)) return;

        if (container) container.IsCapped = true;

        // Freeze the cap while snapped
        if (args.interactableObject is UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab)
        {
            var rb = grab.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            grab.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.Kinematic;
        }
    }

    void OnExit(SelectExitEventArgs args)
    {
        if (!IsCap(args.interactableObject)) return;

        if (container) container.IsCapped = false;

        if (args.interactableObject is UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab)
        {
            var rb = grab.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
            grab.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.VelocityTracking;
        }
    }
}
