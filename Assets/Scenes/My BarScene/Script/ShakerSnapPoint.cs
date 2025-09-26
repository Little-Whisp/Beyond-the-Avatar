using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor))]
public class ShakerSnapPoint : MonoBehaviour
{
    [Header("Refs")]
    public ShakerContainer container;
    public ShakerPourDetector pourer;

    [Header("Collisions (optional)")]
    public bool ignoreCupVsLidWhileSnapped = true;

    UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;
    Collider[] cupCols;
    readonly List<(Collider a, Collider b)> ignored = new();

    void Awake()
    {
        socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        socket.selectEntered.AddListener(OnSelectEntered);
        socket.selectExited.AddListener(OnSelectExited);

        cupCols = GetComponentsInParent<Collider>(true);
    }

    void OnDestroy()
    {
        if (!socket) return;
        socket.selectEntered.RemoveListener(OnSelectEntered);
        socket.selectExited.RemoveListener(OnSelectExited);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        // If the socket selected it, it’s the lid. No extra checks needed.
        if (container) container.IsSealed = true;

        if (args.interactableObject is XRGrabInteractable grab)
        {
            var rb = grab.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.SetLinVel(Vector3.zero);
                rb.angularVelocity = Vector3.zero;
            }
            // grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        }

        if (ignoreCupVsLidWhileSnapped && args.interactableObject is XRGrabInteractable grab3)
        {
            var lidCols = grab3.GetComponentsInChildren<Collider>(true);
            ToggleIgnore(lidCols, cupCols, true);
        }
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        if (container && container.HasLiquid)
        {
            Resnap(args.interactableObject as XRGrabInteractable);
            return;
        }

        if (container) container.IsSealed = false;

        var grab = args.interactableObject as XRGrabInteractable;
        if (grab)
        {
            var rb = grab.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
            // grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        }

        if (ignoreCupVsLidWhileSnapped && grab)
        {
            var lidCols = grab.GetComponentsInChildren<Collider>(true);
            ToggleIgnore(lidCols, cupCols, false);
        }
    }

    void Resnap(XRGrabInteractable grab)
    {
        if (!grab) return;
        var attach = socket.attachTransform ? socket.attachTransform : socket.transform;
        grab.transform.SetPositionAndRotation(attach.position, attach.rotation);

        var rb = grab.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.SetLinVel(Vector3.zero);
            rb.angularVelocity = Vector3.zero;
        }

        if (container) container.IsSealed = true;
    }

    void ToggleIgnore(Collider[] lids, Collider[] cups, bool ignore)
    {
        if (lids == null || cups == null) return;
        if (ignore)
        {
            ignored.Clear();
            foreach (var a in lids)
                foreach (var b in cups)
                    if (a && b && a != b) { Physics.IgnoreCollision(a, b, true); ignored.Add((a, b)); }
        }
        else
        {
            foreach (var p in ignored) if (p.a && p.b) Physics.IgnoreCollision(p.a, p.b, false);
            ignored.Clear();
        }
    }
}
