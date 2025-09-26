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
    public ShakerMixController mix;

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
        if (container) container.IsSealed = true;

        if (args.interactableObject is XRGrabInteractable grab &&
            grab.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.SetLinVel(Vector3.zero);
            rb.angularVelocity = Vector3.zero;
        }

        if (ignoreCupVsLidWhileSnapped && args.interactableObject is XRGrabInteractable g3)
        {
            var lidCols = g3.GetComponentsInChildren<Collider>(true);
            ToggleIgnore(lidCols, cupCols, true);
        }
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        bool fromThisSocket = args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor;
        var grab = args.interactableObject as XRGrabInteractable;

        // Only resnap while actively mixing
        if (fromThisSocket && mix && mix.IsMixing)
        { Resnap(grab); return; }

        if (container) container.IsSealed = false;

        if (grab && grab.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.useGravity = true;
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

        if (grab.TryGetComponent<Rigidbody>(out var rb))
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
