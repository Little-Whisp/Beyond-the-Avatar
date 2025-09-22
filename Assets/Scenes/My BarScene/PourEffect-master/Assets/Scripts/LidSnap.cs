using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class LidSnap : MonoBehaviour
{
    [Header("References")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable lidGrab;  // the lid's XRGrabInteractable
    public Transform snapPoint;         // where the lid ends up when locked
    public Transform bodyRoot;          // usually the Body transform (parent to attach under when locked)

    [Header("Tuning")]
    public float snapSpeed = 12f;       // how quickly it flies to the snap point
    public float posThreshold = 0.005f; // done snapping when closer than this
    public float rotThreshold = 1f;     // degrees

    [Header("Optional Highlight")]
    public Renderer[] highlightRenderers; // set renderers to glow/show when lid is in range

    Rigidbody lidRB;
    bool lidInZone = false;
    bool snapping = false;
    bool locked = false;

    void Awake()
    {
        if (!lidGrab) { Debug.LogError("LidSnapZone: lidGrab not set."); enabled = false; return; }
        lidRB = lidGrab.GetComponent<Rigidbody>();

        // XR events
        lidGrab.selectEntered.AddListener(OnLidGrabbed);
        lidGrab.selectExited.AddListener(OnLidReleased);
    }

    void OnDestroy()
    {
        // clean up listeners
        if (lidGrab != null)
        {
            lidGrab.selectEntered.RemoveListener(OnLidGrabbed);
            lidGrab.selectExited.RemoveListener(OnLidReleased);
        }
    }

    // --- Trigger detection ---
    void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == lidRB)
        {
            lidInZone = true;
            SetHighlight(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody == lidRB)
        {
            lidInZone = false;
            SetHighlight(false);
            // cancel any pending snap if it leaves the zone
            if (!locked) snapping = false;
        }
    }

    // --- XR grab callbacks ---
    void OnLidGrabbed(SelectEnterEventArgs _)
    {
        // If it was locked, detach for free movement
        if (locked)
        {
            locked = false;
            lidRB.isKinematic = false;
            lidGrab.transform.SetParent(null, true);
        }
        // If it was snapping, cancel
        snapping = false;
    }

    void OnLidReleased(SelectExitEventArgs _)
    {
        // Only start snap if the lid is in the zone when released
        if (lidInZone && !locked)
        {
            snapping = true;
            lidRB.isKinematic = true; // take control during snap
        }
    }

    // --- Snapping motion (use physics-friendly MovePosition/Rotation) ---
    void FixedUpdate()
    {
        if (!snapping) return;

        Transform t = lidRB.transform;

        Vector3 p = Vector3.Lerp(t.position, snapPoint.position, Time.fixedDeltaTime * snapSpeed);
        Quaternion r = Quaternion.Slerp(t.rotation, snapPoint.rotation, Time.fixedDeltaTime * snapSpeed);

        lidRB.MovePosition(p);
        lidRB.MoveRotation(r);

        bool closePos = Vector3.Distance(p, snapPoint.position) <= posThreshold;
        bool closeRot = Quaternion.Angle(r, snapPoint.rotation) <= rotThreshold;

        if (closePos && closeRot)
        {
            // Finalize lock
            snapping = false;
            locked = true;

            t.SetParent(bodyRoot, true);
            t.position = snapPoint.position;
            t.rotation = snapPoint.rotation;

            SetHighlight(false);
            // keep isKinematic=true while locked so it follows the body perfectly
        }
    }

    void SetHighlight(bool on)
    {
        if (highlightRenderers == null) return;
        foreach (var rend in highlightRenderers)
            if (rend) rend.enabled = on;
    }
}
