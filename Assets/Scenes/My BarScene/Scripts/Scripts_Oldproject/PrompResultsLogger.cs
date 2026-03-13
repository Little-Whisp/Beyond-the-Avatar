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

        bool fileExists = File.Exists(path);

        using (StreamWriter writer = new StreamWriter(path, true))
        {
            // Write header only if file is new
            if (!fileExists)
            {
                writer.WriteLine("Prompt,Avatar,SodaPercent,HotSaucePercent,StrawberryPercent,Timestamp,PlayerID,RoundIndex");
            }

            // SESSION SEPARATOR
            writer.WriteLine("");
            writer.WriteLine($"--- NEW SESSION --- Player: {(results.Count > 0 ? results[0].playerID : "Unknown")}  Start: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss} ---");

            foreach (var entry in results)
            {
                string prompt = (entry.prompt ?? "").Replace("\"", "'").Replace("\n", " ");
                string avatar = (entry.avatarTag ?? "").Replace("\"", "'");
                string timestamp = (entry.timestamp ?? "").Replace("\"", "'");
                string playerID = (entry.playerID ?? "").Replace("\"", "'");

                writer.WriteLine(
                    $"\"{prompt}\"," +
                    $"\"{avatar}\"," +
                    $"{entry.sodaML}," +
                    $"{entry.hotSauceML}," +
                    $"{entry.strawberryML}," +
                    $"\"{timestamp}\"," +
                    $"\"{playerID}\"," +
                    $"{entry.roundIndex}"
                );
            }
        }

        Debug.Log($"[PromptResultsLogger] CSV saved to: {path}");

        results.Clear();

#if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("[PromptResultsLogger] Quest path example:");
        Debug.Log("/sdcard/Android/data/<your.bundle.id>/files/" + fileName);
#endif
    }
}