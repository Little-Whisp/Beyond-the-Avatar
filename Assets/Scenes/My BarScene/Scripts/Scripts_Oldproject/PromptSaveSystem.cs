using System.IO;
using UnityEngine;
using System.Collections.Generic;

public static class PromptSaveSystem
{
    public static string FilePath => Path.Combine(Application.persistentDataPath, "AllResults.json");

    public static void SaveResults(List<PromptResult> currentSession)
    {
        AllPlayerResults all = LoadResults();
        all.allResults.AddRange(currentSession);

        string json = JsonUtility.ToJson(all, true);
        File.WriteAllText(FilePath, json);
        Debug.Log("[PromptSaveSystem] Saved results to: " + FilePath);
    }

    public static AllPlayerResults LoadResults()
    {
        if (!File.Exists(FilePath))
            return new AllPlayerResults();

        string json = File.ReadAllText(FilePath);
        return JsonUtility.FromJson<AllPlayerResults>(json);
    }
}