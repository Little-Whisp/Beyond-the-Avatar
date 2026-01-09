using UnityEngine;
using TMPro;

public class PromptGenerator : MonoBehaviour
{
    public TextMeshProUGUI promptText;
    public PromptManager promptManager;

    [Tooltip("Used to flip antisocial/prosocial order")]
    public int playerIndex;

    public string currentPrompt;

    public void ShowNextPrompt()
    {
        if (promptManager == null || promptText == null)
            return;

        string[] prompts = promptManager.GetPromptsForPlayer(playerIndex);

        // Show first prompt only (you can show both if needed)
        currentPrompt = prompts.Length > 0 ? prompts[0] : "";

        promptText.text = currentPrompt;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        currentPrompt = "";
        gameObject.SetActive(false);
    }
}
