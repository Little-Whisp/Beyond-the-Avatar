using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ResultsUI : MonoBehaviour
{
    public TextMeshProUGUI logText;
    public Score scoreManager;

    public void Start()
    {
        Debug.Log("hiding...");
        gameObject.SetActive(false);
        
    }

    public void DisplayDataBench()
    {
        Debug.Log("showing...");

        gameObject.SetActive(true);

        if (scoreManager == null || logText == null)
        {
            Debug.LogWarning("Missing references in ResultsUI!");
            return;
        }

        if (scoreManager.dataBench.Count == 0)
        {
            logText.text = "No data recorded yet.";
            return;
        }

        // Show final score first
        logText.text = $"<b>Final Score:</b> {scoreManager.GetCurrentScore()}\n\n";

        // Show each prompt entry
        foreach (var entry in scoreManager.dataBench)
        {
            logText.text +=
                $"<b>Prompt:</b> {entry.prompt}\n" +
                $"<b>Avatar:</b> {entry.avatarTag}\n\n";
        }
    }

    public void DisplayComparison()
    {
        AllPlayerResults all = PromptSaveSystem.LoadResults();
        var percentages = CalculatePercentages(all);

        logText.text += "\n<b>Community Results:</b>\n";

        foreach (var prompt in percentages.Keys)
        {
            logText.text += $"<b>{prompt}</b>\n";
            foreach (var avatar in percentages[prompt].Keys)
            {
                float percent = percentages[prompt][avatar];
                logText.text += $"  {avatar}: {percent:F0}%\n";
            }
            logText.text += "\n";
        }
    }

    private Dictionary<string, Dictionary<string, float>> CalculatePercentages(AllPlayerResults data)
    {
        var result = new Dictionary<string, Dictionary<string, float>>();
        var countPerPrompt = new Dictionary<string, int>();

        foreach (var entry in data.allResults)
        {
            if (!result.ContainsKey(entry.prompt))
                result[entry.prompt] = new Dictionary<string, float>();

            if (!result[entry.prompt].ContainsKey(entry.avatarTag))
                result[entry.prompt][entry.avatarTag] = 0;

            result[entry.prompt][entry.avatarTag]++;

            // Fix: Initialize countPerPrompt properly
            if (!countPerPrompt.ContainsKey(entry.prompt))
                countPerPrompt[entry.prompt] = 1;
            else
                countPerPrompt[entry.prompt]++;
        }

        // Convert to percentages
        foreach (var prompt in result.Keys)
        {
            foreach (var avatar in result[prompt].Keys)
            {
                result[prompt][avatar] = (result[prompt][avatar] / countPerPrompt[prompt]) * 100f;
            }
        }

        return result;
    }

}
