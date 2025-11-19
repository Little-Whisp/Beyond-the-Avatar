using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GlassPickup : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable != null)
        {
            Debug.Log("[GlassPickup] XRGrabInteractable found on " + gameObject.name);
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
        else
        {
            Debug.LogWarning("[GlassPickup] No XRGrabInteractable found on " + gameObject.name);
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
        Debug.Log("[GlassPickup] OnGrabbed fired for " + gameObject.name);

        if (ZoneVisualManager.Instance != null)
        {
            Debug.Log("[GlassPickup] Calling ShowAllZones()");
            ZoneVisualManager.Instance.ShowAllZones();
        }
        else
        {
            Debug.LogWarning("[GlassPickup] ZoneVisualManager.Instance is null!");
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        Debug.Log("[GlassPickup] OnReleased fired for " + gameObject.name);

        if (ZoneVisualManager.Instance != null)
        {
            Debug.Log("[GlassPickup] Calling HideAllZones()");
            ZoneVisualManager.Instance.HideAllZones();
        }
        else
        {
            Debug.LogWarning("[GlassPickup] ZoneVisualManager.Instance is null!");
        }
    }
}
