using UnityEngine;

public class PromptTrigger : MonoBehaviour
{
    public PromptManager promptManager;
    public PromptGenerator promptGenerator;

    public string playerTag = "Player";
    private bool hasShownPrompt = false;

    private void OnTriggerEnter(Collider other)
    {
        // Debug: log EVERY collider that enters
        Debug.Log($"[PromptTrigger] OnTriggerEnter from {other.name} (tag={other.tag})");

        if (!other.CompareTag(playerTag))
            return;

        if (hasShownPrompt)
        {
            Debug.Log("[PromptTrigger] Prompt already shown, ignoring.");
            return;
        }

        Debug.Log("[PromptTrigger] Valid player entered, showing prompt.");

        if (promptManager != null)
        {
            string prompt = promptManager.GetNextPrompt();
            Debug.Log("[PromptTrigger] New prompt text: " + prompt);
        }

        if (promptGenerator != null)
            promptGenerator.ShowNextPrompt();

        hasShownPrompt = true;
    }

    public void ResetPrompt()
    {
        hasShownPrompt = false;
        if (promptGenerator != null)
            promptGenerator.Hide();
    }

    public string GetCurrentPrompt()
    {
        return promptGenerator != null ? promptGenerator.currentPrompt : "";
    }
}
