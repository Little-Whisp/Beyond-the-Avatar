using UnityEngine;
using TMPro;

public class PromptGenerator : MonoBehaviour
{
    public TextMeshProUGUI promptText;
    public PromptManager promptManager;

    public string currentPrompt;

    public void ShowNextPrompt()
    {
        currentPrompt = promptManager.GetNextPrompt();
        promptText.text = currentPrompt;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        currentPrompt = "";
        gameObject.SetActive(false);
    }
}
