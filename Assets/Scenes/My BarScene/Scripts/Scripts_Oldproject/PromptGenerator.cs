using UnityEngine;
using TMPro;

public class PromptGenerator : MonoBehaviour
{
    public TextMeshProUGUI promptText;
    public PromptManager promptManager;

    public int playerIndex;

    public string currentPrompt;

    private string[] currentPrompts;
    private int currentPromptIndex = 0;

    public void ShowNextPrompt()
    {
        Debug.Log("ShowNextPrompt CALLED");

        if (promptManager == null || promptText == null)
            return;

        // If we don't have prompts yet OR finished the pair
        if (currentPrompts == null || currentPromptIndex >= currentPrompts.Length)
        {
            currentPrompts = promptManager.GetPromptsForPlayer(playerIndex);
            currentPromptIndex = 0;
        }

        currentPrompt = currentPrompts[currentPromptIndex];
        promptText.text = currentPrompt;
        gameObject.SetActive(true);

        Debug.Log($"Showing prompt: {currentPrompt}");

        currentPromptIndex++;
    }

    public void Hide()
    {
        currentPrompt = "";
        gameObject.SetActive(false);
    }
}