using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PromptResultsLogger : MonoBehaviour
{
    public readonly List<PromptResult> results = new();

    public void Add(PromptResult r) => results.Add(r);

    public void ExportCsv(string fileName = "PromptResults.csv")
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);

        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("Prompt,Avatar,SodaML,HotSauceML,StrawberryML,Timestamp,PlayerID,RoundIndex"); foreach (var entry in results)
            {
                // Basic CSV safety: replace commas/newlines
                string p = (entry.prompt ?? "").Replace(",", " ").Replace("\n", " ").Replace("\r", " ");
                string a = (entry.avatarTag ?? "").Replace(",", " ");
                string t = (entry.timestamp ?? "").Replace(",", " ");
                string id = (entry.playerID ?? "").Replace(",", " ");

                writer.WriteLine($"{p},{a},{entry.sodaML},{entry.hotSauceML},{entry.strawberryML},{t},{id},{entry.roundIndex}");
            }
        }

        Debug.Log($"[PromptResultsLogger] CSV saved to: {path}");

#if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("[PromptResultsLogger] Quest path example:");
        Debug.Log("/sdcard/Android/data/<your.bundle.id>/files/" + fileName);
#endif
    }
}
