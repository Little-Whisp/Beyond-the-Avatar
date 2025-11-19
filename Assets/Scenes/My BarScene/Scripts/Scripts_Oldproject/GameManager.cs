using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Prompt System")]
    public PromptManager promptManager;   // ScriptableObject with GetNextPrompt()
    public PromptTrigger promptTrigger;   // Trigger at the bar

    [Header("Debug")]
    [TextArea]
    public string currentPrompt;          // Just to see it in Inspector

    // --- data logging ---
    private PlayerData currentData;
    private string dataPath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            dataPath = Application.persistentDataPath + "/";
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Start a new logging session
        CreateNewPlaythrough();

        // Reset prompt system so PromptTrigger starts from the beginning
        if (promptManager != null)
            promptManager.ResetCategoryCycle();

        if (promptTrigger != null)
            promptTrigger.ResetPrompt();
    }

    /// <summary>
    /// Called by CustomerDrinkReceiver when a player gives a cocktail to a customer.
    /// </summary>
    public void OnCocktailServed(string customerName, string cocktailName)
    {
        // Ask PromptTrigger what is currently shown
        string promptText = promptTrigger != null
            ? promptTrigger.GetCurrentPrompt()
            : currentPrompt;

        // Keep local copy for debugging / UI
        currentPrompt = promptText;

        string log = $"Served cocktail '{cocktailName}' to {customerName} | Prompt: {currentPrompt}";
        Debug.Log("[GameManager] " + log);
        LogEvent(log);

        // Let the bar be ready for a NEW prompt next time Player 1 enters
        if (promptTrigger != null)
            promptTrigger.ResetPrompt();
    }

    // --------------------------------------------------------------------
    // Logging helpers (same idea as your old GameManager)
    // --------------------------------------------------------------------

    private void CreateNewPlaythrough()
    {
        currentData = new PlayerData
        {
            playerID = Guid.NewGuid().ToString(),
            startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            dataLog = new List<string>()
        };
    }

    public string GetPlayerID()
    {
        return currentData != null ? currentData.playerID : "UnknownPlayer";
    }

    public void LogEvent(string logEntry)
    {
        if (currentData != null)
            currentData.dataLog.Add($"{DateTime.Now:HH:mm:ss} - {logEntry}");
    }

    public void CompletePlaythrough()
    {
        if (currentData != null)
        {
            string json = JsonUtility.ToJson(currentData, true);
            string fileName = $"playthrough_{currentData.playerID}.json";
            File.WriteAllText(Path.Combine(dataPath, fileName), json);
            Debug.Log("[GameManager] Playthrough saved: " + fileName);
        }
    }
}

[System.Serializable]
public class PlayerData
{
    public string playerID;
    public string startTime;
    public List<string> dataLog;
}
