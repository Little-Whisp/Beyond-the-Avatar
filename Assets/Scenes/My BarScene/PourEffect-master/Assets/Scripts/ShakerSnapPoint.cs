using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ShakerSnapPoint : MonoBehaviour
{
    UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;

    void Awake()
    {
        socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        socket.selectEntered.AddListener(OnSelectEntered);
        socket.selectExited.AddListener(OnSelectExited);
    }

    void OnDestroy()
    {
        socket.selectEntered.RemoveListener(OnSelectEntered);
        socket.selectExited.RemoveListener(OnSelectExited);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Lid just snapped in
        if (args.interactableObject is UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab)
        {
            var rb = grab.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = true;               // prevents jiggle while shaking
            grab.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.Kinematic;
        }
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        // Lid left the socket (player pulled it off)
        if (args.interactableObject is UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab)
        {
            var rb = grab.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = false;              // back to normal physics
            grab.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.VelocityTracking;
        }
    }
}
