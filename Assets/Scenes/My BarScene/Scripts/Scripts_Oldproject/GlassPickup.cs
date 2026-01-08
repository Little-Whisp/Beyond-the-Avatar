using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class GlassPickup : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    public TriggerZone[] triggerZones;

    [Header("Rules")]
    public string cocktailTag = "Cocktail"; // only objects with this tag trigger highlight

    private bool allowHighlight = false;   // Blocks auto-highlights on spawn

    // Physics
    private Rigidbody rb;
    private Collider myCol;
    private Collider[] zoneCols;

    private void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        rb = GetComponentInParent<Rigidbody>();
        myCol = GetComponentInParent<Collider>();


        // Cache zone colliders (the trigger colliders on the zones)
        if (triggerZones != null && triggerZones.Length > 0)
        {
            var list = new System.Collections.Generic.List<Collider>();
            foreach (var z in triggerZones)
            {
                if (!z) continue;
                var zc = z.GetComponent<Collider>();
                if (zc && zc.isTrigger) list.Add(zc);   // ✅ only triggers
            }
            zoneCols = list.ToArray();
        }


        allowHighlight = false;
        StartCoroutine(EnableHighlightAfterDelay());

        if (grab != null)
        {
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
        }
        else
        {
            Debug.LogWarning($"[GlassPickup] Awake: no XRGrabInteractable found on {name}");
        }
    }

    // Call this after runtime-decoration to ensure cached refs and listeners are correct.
    public void Reinitialize()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        rb = GetComponentInParent<Rigidbody>();
        myCol = GetComponentInParent<Collider>();

        // Rebuild the trigger zone cache
        RebuildZoneCache();

        // Reset highlight protection
        allowHighlight = false;
        StopAllCoroutines();
        StartCoroutine(EnableHighlightAfterDelay());

        // Ensure listeners are attached exactly once
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
            grab.selectExited.RemoveListener(OnReleased);
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
        }
    }

    public void RebuildZoneCache()
    {
        if (triggerZones == null) return;

        var list = new System.Collections.Generic.List<Collider>();
        foreach (var z in triggerZones)
        {
            if (!z) continue;
            var zc = z.GetComponent<Collider>();
            if (zc && zc.isTrigger)
                list.Add(zc);
        }

        zoneCols = list.ToArray();
    }


    private IEnumerator EnableHighlightAfterDelay()
    {
        yield return new WaitForSeconds(0.25f);
        allowHighlight = true;
        Debug.Log("✔ Highlight now enabled. Auto-grab protection finished.");
    }

    // Prevent the drink from constantly re-triggering placement while held
    private void IgnoreZones(bool ignore)
    {
        if (!myCol || zoneCols == null) return;

        foreach (var zc in zoneCols)
        {
            if (zc) Physics.IgnoreCollision(myCol, zc, ignore);
        }
    }

    // Ensure normal physics when grabbed
    private void ForceNormalPhysicsNow()
    {
        if (!rb) return;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // ✅ ALWAYS do these first (even if highlights are blocked by host/tag checks)
        IgnoreZones(true);
        ForceNormalPhysicsNow();

        Debug.Log($"[GRAB] {name} | tag={tag} | interactor={args.interactorObject?.GetType().Name} | time={Time.time}");

        if (!allowHighlight)
        {
            Debug.Log("Blocked auto-grab highlight.");
            return;
        }

        // bartender-only (host) for highlight visuals
        if (!Unity.Netcode.NetworkManager.Singleton || !Unity.Netcode.NetworkManager.Singleton.IsHost)
            return;

        // only cocktails trigger highlight
        if (!CompareTag(cocktailTag))
            return;

        foreach (var zone in triggerZones)
            zone?.ShowHighlight();
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        // ✅ ALWAYS re-enable zone collisions after release
        IgnoreZones(false);

        // ✅ IMPORTANT: make sure it isn't kinematic at detach/throw time
        // (some things set it kinematic during the same frame; FixedUpdate timing helps)
        StartCoroutine(ForceNonKinematicNextPhysicsTick());

        if (!allowHighlight)
            return;

        // bartender-only (host) for highlight visuals
        if (!Unity.Netcode.NetworkManager.Singleton || !Unity.Netcode.NetworkManager.Singleton.IsHost)
            return;

        // only cocktails trigger hide
        if (!CompareTag(cocktailTag))
            return;

        foreach (var zone in triggerZones)
            zone?.HideHighlight();
    }

    private IEnumerator ForceNonKinematicNextPhysicsTick()
    {
        yield return new WaitForFixedUpdate();
        if (!rb) yield break;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
