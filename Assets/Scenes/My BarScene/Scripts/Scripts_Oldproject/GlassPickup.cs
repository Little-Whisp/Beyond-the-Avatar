using UnityEngine;
// using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;   

public class GlassPickup : MonoBehaviour
{
    private XRGrabInteractable grab;
    public TriggerZone[] triggerZones;

    private bool allowHighlight = false;   // Blocks auto-highlights on spawn

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        // Prevent highlight at start
        allowHighlight = false;
        StartCoroutine(EnableHighlightAfterDelay());

        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    private IEnumerator EnableHighlightAfterDelay()
    {
        // Wait long enough for XR + Netcode to fully finish syncing
        yield return new WaitForSeconds(0.25f);

        allowHighlight = true;
        Debug.Log("✔ Highlight now enabled. Auto-grab protection finished.");
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (!allowHighlight)
        {
            Debug.Log(" Blocked auto-grab highlight.");
            return;
        }

        foreach (var zone in triggerZones)
            zone?.ShowHighlight();
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (!allowHighlight)
            return;

        foreach (var zone in triggerZones)
            zone?.HideHighlight();
    }
}
