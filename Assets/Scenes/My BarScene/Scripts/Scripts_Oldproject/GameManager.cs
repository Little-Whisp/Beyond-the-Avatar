using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Data Logger")]
    public PromptResultsLogger promptLogger;

    [Header("End Experience")]
    public GameObject endExperienceUI;

    [Header("Shaker Reference")]
    public ShakerContainer shaker;

    [Header("Scene References")]
    public GameObject barUI;
    public GameObject interactionRoot;

    [Header("Debug")]
    [TextArea]
    public string currentPrompt;

    private PlayerData currentData;
    private int roundIndex = 0;
    private bool experimentEnded = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        CreateNewPlaythrough();

        if (endExperienceUI != null)
            endExperienceUI.SetActive(false);
    }

    // -----------------------------------------------------
    // MAIN LOGGING FUNCTION
    // -----------------------------------------------------

    public void OnCocktailServed(string avatarType, string drinkType, string prompt)
    {
        if (experimentEnded) return;

        currentPrompt = string.IsNullOrEmpty(prompt) ? "UnknownPrompt" : prompt;
        avatarType = string.IsNullOrEmpty(avatarType) ? "UnknownAvatar" : avatarType;

        Debug.Log($"[GameManager] Drink served | Avatar: {avatarType} | Prompt: {currentPrompt}");

        // --- Calculate ingredient percentages ---
        float sodaPercent = 0f;
        float hotSaucePercent = 0f;
        float strawberryPercent = 0f;

        if (shaker != null)
        {
            sodaPercent = shaker.mlOld;
            hotSaucePercent = shaker.mlLife;
            strawberryPercent = shaker.mlImp;
        }

        if (promptLogger != null)
        {
            PromptResult result = new PromptResult
            {
                prompt = currentPrompt,
                avatarTag = avatarType,

                sodaML = sodaPercent,
                hotSauceML = hotSaucePercent,
                strawberryML = strawberryPercent,

                timestamp = DateTime.UtcNow.ToString("o"),
                playerID = GetPlayerID(),
                roundIndex = roundIndex
            };

            promptLogger.Add(result);
        }
        else
        {
            Debug.LogWarning("[GameManager] PromptLogger is not assigned.");
        }

        roundIndex++;

        // Clear shaker for next drink
        if (shaker != null)
        {
            shaker.Clear();
        }
    }

    // -----------------------------------------------------
    // END EXPERIENCE
    // -----------------------------------------------------

    public void FinishExperience()
    {
        Debug.Log("FINISH EXPERIENCE CALLED");
        if (experimentEnded) return;

        experimentEnded = true;
        Debug.Log("[GameManager] Experiment finished.");

        if (promptLogger != null)
        {
            Debug.Log("[GameManager] Exporting CSV...");
            promptLogger.ExportCsv();
        }
        else
        {
            Debug.LogWarning("[GameManager] PromptLogger is not assigned, CSV not exported.");
        }

        if (barUI != null)
            barUI.SetActive(false);

        if (interactionRoot != null)
            interactionRoot.SetActive(false);

        if (endExperienceUI != null)
            endExperienceUI.SetActive(true);
    }

    // -----------------------------------------------------
    // SESSION DATA
    // -----------------------------------------------------

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
}

[Serializable]
public class PlayerData
{
    public string playerID;
    public string startTime;
    public List<string> dataLog;
}