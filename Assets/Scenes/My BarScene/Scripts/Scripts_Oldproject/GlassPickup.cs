using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GlassPickup : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private Outline outline;

    public TriggerZone[] triggerZones;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        outline = GetComponent<Outline>();

        if (outline != null)
            outline.enabled = false;

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log($"[GlassPickup] Picked up {gameObject.name}");

        // Hide everything first (clean slate)
        ZoneVisualManager.Instance?.HideAllZones();

        // Then highlight the correct zones
        if (triggerZones != null)
        {
            foreach (var zone in triggerZones)
            {
                if (zone != null)
                {
                    Debug.Log($"[GlassPickup] Highlight zone: {zone.name}");
                    zone.OnGlassPickedUp(gameObject);
                }
            }
        }

        if (outline != null)
            outline.enabled = false;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        Debug.Log($"[GlassPickup] Released {gameObject.name}");

        // Hide all zones when the glass is let go
        ZoneVisualManager.Instance?.HideAllZones();

        if (outline != null)
            outline.enabled = false;
    }
}
