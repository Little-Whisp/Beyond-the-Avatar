using System.IO;
using UnityEngine;

public class Exporter : MonoBehaviour
{
    public Score scoreManager;

    public void ExportDataBench()
    {
        string fileName = "PromptResults.csv";
        string path = Path.Combine(Application.persistentDataPath, fileName);

        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("Prompt,Avatar,Timestamp,PlayerID");
            foreach (var entry in scoreManager.dataBench)
            {
                writer.WriteLine($"{entry.prompt},{entry.avatarTag},{entry.timestamp},{entry.playerID}");
            }
        }

        Debug.Log($"[Exporter] File saved to: {path}");

#if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("[Exporter] You can find this file on your Quest 3 at:");
        Debug.Log("/sdcard/Android/data/com.UnityTechnologies.com.unity.template.urpblank/files/" + fileName);
#endif
    }
}
