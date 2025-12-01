using UnityEngine;
using Unity.Netcode;

public class TriggerZone : MonoBehaviour
{
    [Header("Basic Settings")]
    public bool isGlassZone = true;
    public string drinkTag = "Cocktail";

    [Header("Prompt System")]
    public PromptTrigger promptTrigger;

    [Header("Player Base (dynamic)")]
    public PlayerBaseTrigger playerBase;

    [Header("Visual (Highlight)")]
    public GameObject zoneVisual;

    public Transform glassAnchor;

    private GameObject lastPlacedGlass;

    private void Awake()
    {
        if (zoneVisual != null)
            zoneVisual.SetActive(false);
    }


    private void OnNetworkSpawn()
    {
        if (zoneVisual != null)
        {
            zoneVisual.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isGlassZone || !other.CompareTag(drinkTag))
            return;

        HandleGlassPlacement(other.gameObject);
    }

    private void HandleGlassPlacement(GameObject glass)
    {
        lastPlacedGlass = glass;

        if (glassAnchor != null)
        {
            glass.transform.position = glassAnchor.position;
            glass.transform.rotation = glassAnchor.rotation;
        }

        var rb = glass.GetComponent<Rigidbody>();
        var col = glass.GetComponent<Collider>();

        if (rb != null) rb.isKinematic = true;
        if (col != null) col.enabled = true;

        if (zoneVisual != null)
            zoneVisual.SetActive(false);

        string owner = playerBase != null ? playerBase.assignedPlayerName : "Unknown";
        string avatar = playerBase != null ? playerBase.assignedAvatarType : "Unknown";

        string prompt = promptTrigger?.promptGenerator?.currentPrompt ?? "UnknownPrompt";

        Debug.Log($"[DATA] Player: {owner} | Avatar: {avatar} | Prompt: {prompt}");

        GameManager.Instance?.LogEvent(
            $"Served -> Player: {owner} | Avatar: {avatar} | Prompt: {prompt}"
        );

        promptTrigger?.ResetPrompt();
    }

    public void ShowHighlight()
    {
        Debug.Log($"ShowHighlight() on {name}");
        if (zoneVisual != null)
            zoneVisual.SetActive(true);
    }

    public void HideHighlight()
    {
        if (zoneVisual != null)
            zoneVisual.SetActive(false);
    }
}
