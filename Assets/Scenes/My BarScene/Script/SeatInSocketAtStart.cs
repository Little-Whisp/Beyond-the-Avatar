using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SeatInSocketAtStart : MonoBehaviour
{
    public XRGrabInteractable interactableToSeat; // the cap (or lid)
    public Rigidbody rbToSeat;                    // same object's RB
    public Transform interactableAlign;           // e.g., CapAlign on the cap
    public bool kinematicWhileSeated = true;

    void Awake()
    {
        if (!interactableToSeat) return;

        var socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        var seat   = socket && socket.attachTransform ? socket.attachTransform : transform;

        // If you gave us an align point, align THAT to the seat
        if (interactableAlign)
        {
            // Move the cap so its align point coincides with the seat
            var t = interactableToSeat.transform;
            var offset = t.position - interactableAlign.position;
            t.position = seat.position + offset;
            t.rotation = seat.rotation * Quaternion.Inverse(interactableAlign.rotation) * t.rotation;
        }
        else
        {
            // Fallback: pivot-to-seat
            interactableToSeat.transform.SetPositionAndRotation(seat.position, seat.rotation);
        }

        if (!rbToSeat) rbToSeat = interactableToSeat.GetComponent<Rigidbody>();
        if (rbToSeat)
        {
            rbToSeat.isKinematic = kinematicWhileSeated;
            rbToSeat.linearVelocity = Vector3.zero;
            rbToSeat.angularVelocity = Vector3.zero;
        }
    }
}