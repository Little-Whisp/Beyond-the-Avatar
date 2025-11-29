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

        // Force-disable every zone visual in scene, even if not assigned
        foreach (var zone in FindObjectsOfType<TriggerZone>(true))
        {
            if (zone.zoneVisual != null)
            {
                Debug.Log($"[ZoneVisualManager] Awake: Disabling zoneVisual for zone {zone.name}");
                zone.zoneVisual.SetActive(false);
            }
        }
    }

    public void ShowAllZones()
    {
        Debug.Log("[ZoneVisualManager] ShowAllZones() called");
        if (allZones == null)
        {
            Debug.LogWarning("[ZoneVisualManager] allZones is null");
            return;
        }

        foreach (var zone in allZones)
        {
            if (zone != null)
            {
                if (zone.zoneVisual != null)
                {
                    Debug.Log($"[ZoneVisualManager] Enabling zoneVisual for zone {zone.name}");
                    zone.zoneVisual.SetActive(true);
                }
                else
                {
                    Debug.LogWarning($"[ZoneVisualManager] zoneVisual is null for zone {zone.name}");
                }
            }
            else
            {
                Debug.LogWarning("[ZoneVisualManager] zone is null in allZones array");
            }
        }
    }

    public void HideAllZones()
    {
        Debug.Log("[ZoneVisualManager] HideAllZones() called");
        if (allZones == null)
        {
            Debug.LogWarning("[ZoneVisualManager] allZones is null");
            return;
        }

        foreach (var zone in allZones)
        {
            if (zone != null)
            {
                if (zone.zoneVisual != null)
                {
                    Debug.Log($"[ZoneVisualManager] Disabling zoneVisual for zone {zone.name}");
                    zone.zoneVisual.SetActive(false);
                }
                else
                {
                    Debug.LogWarning($"[ZoneVisualManager] zoneVisual is null for zone {zone.name}");
                }
            }
            else
            {
                Debug.LogWarning("[ZoneVisualManager] zone is null in allZones array");
            }
        }
    }
}
