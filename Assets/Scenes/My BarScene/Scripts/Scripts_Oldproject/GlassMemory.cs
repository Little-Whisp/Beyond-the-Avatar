using System.Collections.Generic;
using UnityEngine;

public class GlassMemory : MonoBehaviour
{
    public static GlassMemory Instance;

    private HashSet<string> currentMix = new HashSet<string>();

    private void Awake()
    {
        // Make sure there's only one
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetCurrentMix(HashSet<string> mix)
    {
        currentMix = new HashSet<string>(mix);
        Debug.Log("Saved current drink mix: " + string.Join(", ", currentMix));
    }

    public HashSet<string> GetCurrentMix()
    {
        return new HashSet<string>(currentMix);
    }
}
