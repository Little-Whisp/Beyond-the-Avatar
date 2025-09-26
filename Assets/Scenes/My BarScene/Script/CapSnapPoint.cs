using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSocketInteractor))]
public class CapSnapPoint : MonoBehaviour
{
    public ShakerContainer container;   // optional: flips IsCapped for your game logic

    // Put this component on Shaker_Lid (or just keep SnapPoint_Cap under Shaker_Lid)
    public class Shaker_LidMarker : MonoBehaviour { }

    [Header("Resnap Rules")]
    [Tooltip("If true, the cap will pop back to the seat while the lid is seated on the shaker body.")]
    public bool lockWhileLidSeated = true;

    [Tooltip("If true, the cap will pop back to the seat while the container has liquid.")]
    public bool lockWhileHasLiquid = true;

    XRSocketInteractor socket;
    int capLayer;

    Collider[] cupCols;
    Collider[] lidCols;
    readonly List<(Collider a, Collider b)> ignored = new();

    void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
        socket.selectEntered.AddListener(OnEnter);
        socket.selectExited.AddListener(OnExit);

        // lid colliders (as you already do)
        var lidGo = GetComponentInParent<Shaker_LidMarker>()?.gameObject ?? transform.parent?.gameObject;
        lidCols = lidGo ? lidGo.GetComponentsInChildren<Collider>(true) : null;

        // cup (shaker body) colliders – get them from your container root
        // If you have a direct ref, use that instead.
        var cupGo = container ? container.transform.root.gameObject : null;
        cupCols = cupGo ? cupGo.GetComponentsInChildren<Collider>(true) : null;
    }

    void OnDestroy()
    {
        if (!socket) return;
        socket.selectEntered.RemoveListener(OnEnter);
        socket.selectExited.RemoveListener(OnExit);
    }

    bool IsCap(IXRInteractable x)
    {
        var go = (x as Component)?.gameObject;
        return go && (capLayer >= 0) && go.layer == capLayer;
    }

    // ---------- Resnap helpers ----------
    bool ShouldResnap()
    {
        if (!container) return false;
        if (lockWhileHasLiquid && container.HasLiquid) return true;
        if (lockWhileLidSeated && container.IsSealed) return true;  // lid seated on shaker body
        return false;
    }

    void Resnap(XRGrabInteractable grab)
    {
        if (!grab) return;

        // move to the socket's attach point
        var attach = socket.attachTransform ? socket.attachTransform : socket.transform;
        grab.transform.SetPositionAndRotation(attach.position, attach.rotation);

        // keep it locked to the seat
        var rb = grab.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.SetLinVel(Vector3.zero);
            rb.angularVelocity = Vector3.zero;
        }

        if (container) container.IsCapped = true;
    }
    // ------------------------------------

    void OnEnter(SelectEnterEventArgs args)
    {
        if (!IsCap(args.interactableObject)) return;

        if (container) container.IsCapped = true;

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

            // Ignore collisions that cause shake
            var capCols = grab.GetComponentsInChildren<Collider>(true);
            ToggleIgnore(capCols, lidCols, true);     // cap ↔ lid
            ToggleIgnore(capCols, cupCols, true);     // cap ↔ cup  (NEW)
        }
    }

    void OnExit(SelectExitEventArgs args)
    {
        if (!IsCap(args.interactableObject)) return;

        bool exitedFromThisSocket = args.interactorObject is XRSocketInteractor;
        var grab = args.interactableObject as XRGrabInteractable;

        if (exitedFromThisSocket && ShouldResnap())
        {
            Resnap(grab);
            return;
        }

        if (container) container.IsCapped = false;

        if (grab)
        {
            var rb = grab.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.SetLinVel(Vector3.zero);
                rb.angularVelocity = Vector3.zero;
            }

            var capCols = grab.GetComponentsInChildren<Collider>(true);
            ToggleIgnore(capCols, lidCols, false);    // re-enable
            ToggleIgnore(capCols, cupCols, false);    // re-enable (NEW)
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
                {
                    if (!a || !b || a == b) continue;
                    Physics.IgnoreCollision(a, b, true);
                    ignored.Add((a, b));
                }
        }
        else
        {
            foreach (var p in ignored)
                if (p.a && p.b) Physics.IgnoreCollision(p.a, p.b, false);
            ignored.Clear();
        }
    }
}
