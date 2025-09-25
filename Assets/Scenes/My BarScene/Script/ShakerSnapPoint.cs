using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSocketInteractor))]
public class ShakerSnapPoint : MonoBehaviour
{
    [Header("Refs")]
    public ShakerContainer container;           // cup's ShakerContainer
    public ShakerPourDetector pourer;           // on the cup (spawns stream)

    [Header("Lid setup")]
    public string pourOriginName = "PourOrigin"; // child on the lid

    [Header("Collisions (optional)")]
    public bool ignoreCupVsLidWhileSnapped = true;

    XRSocketInteractor socket;
    Collider[] cupCols;
    List<(Collider a, Collider b)> ignored = new();

    // cache layer index to avoid repeated NameToLayer calls
    int lidLayer;

    void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
        socket.selectEntered.AddListener(OnSelectEntered);
        socket.selectExited .AddListener(OnSelectExited);

        cupCols = GetComponentsInParent<Collider>(true);

        // The Interaction Layer Mask on the socket is already set to only accept LID,
        // but we also guard in code as an extra safety net:
        lidLayer = LayerMask.NameToLayer("lidonly");
        if (lidLayer < 0)
            Debug.LogWarning("Layer 'lidonly' not found. Make sure the lid is on that layer.");
    }

    void OnDestroy()
    {
        if (socket)
        {
            socket.selectEntered.RemoveListener(OnSelectEntered);
            socket.selectExited .RemoveListener(OnSelectExited);
        }
    }

    bool IsLid(IXRInteractable x)
    {
        var go = (x as Component)?.gameObject;
        return go && (lidLayer >= 0) && go.layer == lidLayer;
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (!IsLid(args.interactableObject)) return; // ✅ only handle the lid

        if (container) container.IsSealed = true;

        // 1) Set pour origin from the lid's child
        if (pourer && args.interactableObject is XRGrabInteractable grab)
        {
            Transform origin = grab.transform.Find(pourOriginName);
            pourer.origin = origin ? origin : null;
        }

        // 2) Seat lid: freeze physics while attached
        if (args.interactableObject is XRGrabInteractable grab2)
        {
            var rb = grab2.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;        // FIX: use velocity
                rb.angularVelocity = Vector3.zero;
            }
            grab2.movementType = XRBaseInteractable.MovementType.Kinematic;
        }

        // 3) Optional: ignore collisions lid ↔ cup to avoid jitter
        if (ignoreCupVsLidWhileSnapped && args.interactableObject is XRGrabInteractable grab3)
        {
            var lidCols = grab3.GetComponentsInChildren<Collider>(true);
            ToggleIgnore(lidCols, cupCols, true);
        }
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        if (!IsLid(args.interactableObject)) return; // ✅ only handle the lid

        var grab = args.interactableObject as XRGrabInteractable;

        // Block removal while there is still liquid (design rule)
        if (container && container.HasLiquid)
        {
            Resnap(grab);
            return;
        }

        // OK to remove
        if (container) container.IsSealed = false;
        if (pourer)    pourer.origin = null;

        if (grab)
        {
            var rb = grab.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        }

        // re-enable collisions
        if (ignoreCupVsLidWhileSnapped && grab)
        {
            var lidCols = grab.GetComponentsInChildren<Collider>(true);
            ToggleIgnore(lidCols, cupCols, false);
        }
    }

    void Resnap(XRGrabInteractable grab)
    {
        if (!grab) return;

        // Hard-align to socket pose so it "snaps back"
        var attach = socket.attachTransform ? socket.attachTransform : socket.transform;
        grab.transform.SetPositionAndRotation(attach.position, attach.rotation);

        // Make sure it stays seated
        var rb = grab.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;           // FIX
            rb.angularVelocity = Vector3.zero;
        }

        // Keep IsSealed true and origin valid
        if (container) container.IsSealed = true;
        if (pourer)
        {
            var t = grab.transform.Find(pourOriginName);
            pourer.origin = t ? t : null;
        }
    }

    void ToggleIgnore(Collider[] lids, Collider[] cups, bool ignore)
    {
        if (lids == null || cups == null) return;

        if (ignore)
        {
            ignored.Clear();
            foreach (var a in lids)
                foreach (var b in cups)
                {
                    if (!a || !b || a == b) continue;
                    Physics.IgnoreCollision(a, b, true);
                    ignored.Add((a, b));
                }
        }
        else
        {
            foreach (var p in ignored)
            {
                if (p.a && p.b) Physics.IgnoreCollision(p.a, p.b, false);
            }
            ignored.Clear();
        }
    }
}
