using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSocketInteractor))]
public class CapSnapPoint : MonoBehaviour
{
    public ShakerContainer container;
    public ShakerMixController mix;

    XRSocketInteractor socket;

    Collider[] cupCols;
    Collider[] lidCols;
    readonly List<(Collider a, Collider b)> ignored = new();

    void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
        socket.selectEntered.AddListener(OnEnter);
        socket.selectExited.AddListener(OnExit);

        // lid colliders (the cap sits on the lid)
        var lidGo = transform.parent ? transform.parent.gameObject : null;
        lidCols = lidGo ? lidGo.GetComponentsInChildren<Collider>(true) : null;

        // cup colliders (the shaker body)
        var cupGo = container ? container.transform.root.gameObject : null;
        cupCols = cupGo ? cupGo.GetComponentsInChildren<Collider>(true) : null;
    }

    void OnDestroy()
    {
        if (!socket) return;
        socket.selectEntered.RemoveListener(OnEnter);
        socket.selectExited.RemoveListener(OnExit);
    }

    bool ShouldResnap() => mix && mix.IsMixing;

    void Resnap(XRGrabInteractable grab)
    {
        if (!grab) return;
        var attach = socket.attachTransform ? socket.attachTransform : socket.transform;
        grab.transform.SetPositionAndRotation(attach.position, attach.rotation);

        if (grab.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (container) container.IsCapped = true;
    }

    void OnEnter(SelectEnterEventArgs args)
    {
        if (container) container.IsCapped = true;

        if (args.interactableObject is XRGrabInteractable grab &&
            grab.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            var capCols = grab.GetComponentsInChildren<Collider>(true);
            ToggleIgnore(capCols, lidCols, true); // cap ↔ lid
            ToggleIgnore(capCols, cupCols, true); // cap ↔ cup
        }
    }

    void OnExit(SelectExitEventArgs args)
    {
        bool fromThisSocket = args.interactorObject is XRSocketInteractor;
        var grab = args.interactableObject as XRGrabInteractable;

        if (fromThisSocket && ShouldResnap())
        { Resnap(grab); return; }

        if (container) container.IsCapped = false;

        if (grab && grab.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (grab)
        {
            var capCols = grab.GetComponentsInChildren<Collider>(true);
            ToggleIgnore(capCols, lidCols, false);
            ToggleIgnore(capCols, cupCols, false);
        }
    }

    void ToggleIgnore(Collider[] aSet, Collider[] bSet, bool ignore)
    {
        if (aSet == null || bSet == null) return;

        if (ignore)
        {
            ignored.Clear();
            foreach (var a in aSet)
                foreach (var b in bSet)
                    if (a && b && a != b) { Physics.IgnoreCollision(a, b, true); ignored.Add((a, b)); }
        }
        else
        {
            foreach (var p in ignored)
                if (p.a && p.b) Physics.IgnoreCollision(p.a, p.b, false);
            ignored.Clear();
        }
    }
}
