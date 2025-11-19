using UnityEngine;

public class ZoneVisualManager : MonoBehaviour
{
    public static ZoneVisualManager Instance;

    [Header("All trigger zones that have a visual")]
    public TriggerZone[] allZones;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Start hidden
        HideAllZones();
    }

    public void ShowAllZones()
    {
        Debug.Log("[ZoneVisualManager] ShowAllZones()");

        if (allZones == null) return;

        foreach (var zone in allZones)
        {
            if (zone != null && zone.zoneVisual != null)
                zone.zoneVisual.SetActive(true);
        }
    }

    public void HideAllZones()
    {
        Debug.Log("[ZoneVisualManager] HideAllZones()");

        if (allZones == null) return;

        foreach (var zone in allZones)
        {
            if (zone != null && zone.zoneVisual != null)
                zone.zoneVisual.SetActive(false);
        }
    }
}
