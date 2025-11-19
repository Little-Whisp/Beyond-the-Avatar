using UnityEngine;

public class TriggerZone : MonoBehaviour
{
    [Header("Basic Settings")]
    public bool isGlassZone = true;
    public string drinkTag = "Cocktail";   // must match spawned drink tag

    [Header("Prompt System")]
    public PromptTrigger promptTrigger;     // drag your PromptTrigger here

    [Header("Who this zone belongs to")]
    public string playerName = "P2 (Customer)"; // set this per zone in Inspector

    [Header("Visual (optional)")]
    public GameObject zoneVisual;           // highlight / ring / icon for this zone

    private GameObject lastPlacedGlass;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[TriggerZone] OnTriggerEnter: " + other.gameObject.name);

        if (!isGlassZone || !other.CompareTag(drinkTag))
            return;

        HandleGlassPlacement(other.gameObject);
    }

    private void HandleGlassPlacement(GameObject glass)
    {
        lastPlacedGlass = glass;

        // Freeze the glass (optional)
        var rb = glass.GetComponent<Rigidbody>();
        var col = glass.GetComponent<Collider>();
        var grab = glass.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (rb != null) rb.isKinematic = true;
        if (col != null) col.enabled = false;
        if (grab != null) grab.enabled = false;

        if (zoneVisual != null)
            zoneVisual.SetActive(true);

        string currentPrompt = (promptTrigger != null && promptTrigger.promptGenerator != null)
            ? promptTrigger.promptGenerator.currentPrompt
            : "UnknownPrompt";

        Debug.Log($"[DATA] Player: {playerName} | Prompt: {currentPrompt}");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LogEvent(
                $"Served -> Player: {playerName} | Prompt: {currentPrompt}"
            );
        }

        promptTrigger?.ResetPrompt();
    }
}
