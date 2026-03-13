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
            // Header
            writer.WriteLine("Prompt,Avatar,SodaML,HotSauceML,StrawberryML,Timestamp,PlayerID,RoundIndex");

            foreach (var entry in results)
            {
                // Sanitize text
                string prompt = (entry.prompt ?? "").Replace("\"", "'").Replace("\n", " ").Replace("\r", " ");
                string avatar = (entry.avatarTag ?? "").Replace("\"", "'");
                string timestamp = (entry.timestamp ?? "").Replace("\"", "'");
                string playerID = (entry.playerID ?? "").Replace("\"", "'");

                // Format numbers safely
                string soda = entry.sodaML.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string hot = entry.hotSauceML.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string strawberry = entry.strawberryML.ToString(System.Globalization.CultureInfo.InvariantCulture);

                // Write CSV row
                writer.WriteLine(
                    $"\"{prompt}\"," +
                    $"\"{avatar}\"," +
                    $"{soda}," +
                    $"{hot}," +
                    $"{strawberry}," +
                    $"\"{timestamp}\"," +
                    $"\"{playerID}\"," +
                    $"{entry.roundIndex}"
                );
            }
        }

        Debug.Log($"[PromptResultsLogger] CSV saved to: {path}");

#if UNITY_ANDROID && !UNITY_EDITOR
    Debug.Log("[PromptResultsLogger] Quest path example:");
    Debug.Log("/sdcard/Android/data/<your.bundle.id>/files/" + fileName);
#endif
    }
}
