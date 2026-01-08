using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class GlassPickup : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    public TriggerZone[] triggerZones;

    [Header("Rules")]
    public string cocktailTag = "Cocktail"; // ✅ only objects with this tag trigger highlight

    private bool allowHighlight = false;   // Blocks auto-highlights on spawn

    // ✅ NEW: cache rigidbody so we can unfreeze when grabbed again
    private Rigidbody rb;

    private void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        allowHighlight = false;
        StartCoroutine(EnableHighlightAfterDelay());

        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    private IEnumerator EnableHighlightAfterDelay()
    {
        yield return new WaitForSeconds(0.25f);
        allowHighlight = true;
        Debug.Log("✔ Highlight now enabled. Auto-grab protection finished.");
    }

    // ✅ NEW: allow XR to move/throw correctly after the drink was frozen in a TriggerZone
    private void UnfreezeForGrab()
    {
        if (!rb) return;

        rb.isKinematic = false;                // ✅ important
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // ✅ IMPORTANT: unfreeze first, otherwise it can "float" / behave weird
        UnfreezeForGrab();

        Debug.Log(
            $"[GRAB] {name} | tag={tag} | interactor={args.interactorObject?.GetType().Name} | time={Time.time}"
        );

        if (!allowHighlight)
        {
            Debug.Log("Blocked auto-grab highlight.");
            return;
        }

        // ✅ bartender-only (host)
        if (!Unity.Netcode.NetworkManager.Singleton || !Unity.Netcode.NetworkManager.Singleton.IsHost)
            return;

        // ✅ only cocktails trigger highlight
        if (!CompareTag(cocktailTag))
            return;

        foreach (var zone in triggerZones)
            zone?.ShowHighlight();
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (!allowHighlight)
            return;

        // ✅ bartender-only (host)
        if (!Unity.Netcode.NetworkManager.Singleton || !Unity.Netcode.NetworkManager.Singleton.IsHost)
            return;

        // ✅ only cocktails trigger hide (so glasses etc don’t affect zones)
        if (!CompareTag(cocktailTag))
            return;

        foreach (var zone in triggerZones)
            zone?.HideHighlight();
    }
}
