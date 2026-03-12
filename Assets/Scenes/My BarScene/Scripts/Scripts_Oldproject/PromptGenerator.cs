using UnityEngine;
using TMPro;

public class PromptGenerator : MonoBehaviour
{
    public TextMeshProUGUI promptText;
    public PromptManager promptManager;

    [Header("Audio")]
    public AudioSource promptAudioSource;
    public AudioClip[] promptClips;

    public int playerIndex;
    public string currentPrompt;

    private string[] currentPrompts;
    private int currentPromptIndex = 0;
    private int currentDisplayedClipIndex = -1;

   
    public void ShowNextPrompt()
    {
        Debug.Log("ShowNextPrompt CALLED");

        if (promptManager == null || promptText == null)
            return;

        // First time loading prompts
        if (currentPrompts == null)
        {
            currentPrompts = promptManager.GetPromptsForPlayer(playerIndex);
            currentPromptIndex = 0;
        }

        // If we ran out of prompts → finish experiment
        if (currentPromptIndex >= currentPrompts.Length)
        {
            Debug.Log("[PromptGenerator] No more prompts.");

            Hide();

            if (GameManager.Instance != null)
                GameManager.Instance.FinishExperience();

            return;
        }

        currentPrompt = currentPrompts[currentPromptIndex];
        promptText.text = currentPrompt;
        gameObject.SetActive(true);

        currentDisplayedClipIndex = currentPromptIndex;
        PlayAudio();

        currentPromptIndex++;
    }

    void PlayAudio()
    {
        if (promptAudioSource == null || promptClips == null)
            return;

        if (currentDisplayedClipIndex < 0 || currentDisplayedClipIndex >= promptClips.Length)
            return;

        AudioClip clip = promptClips[currentDisplayedClipIndex];
        if (clip == null)
            return;

        promptAudioSource.Stop();
        promptAudioSource.clip = clip;
        promptAudioSource.Play();
    }

    public bool HasMorePromptsInCurrentPair()
    {
        return currentPrompts != null && currentPromptIndex < currentPrompts.Length;
    }

    public void Hide()
    {
        currentPrompt = "";

        if (promptText != null)
            promptText.text = "";

        if (promptAudioSource != null && promptAudioSource.isPlaying)
            promptAudioSource.Stop();

        currentDisplayedClipIndex = -1;

        gameObject.SetActive(false);
    }
}