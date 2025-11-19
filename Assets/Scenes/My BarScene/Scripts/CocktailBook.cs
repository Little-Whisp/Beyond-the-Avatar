using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Bar/Cocktail Book", fileName = "CocktailBook")]
public class CocktailBook : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string name;
        public GameObject prefab;
        [Range(0,1)] public float targetOld  = 0.33f; // OldTwitter
        [Range(0,1)] public float targetLife = 0.33f; // LifeJacket
        [Range(0,1)] public float targetImp  = 0.34f; // Imposter
        [Range(0f,2f)] public float baseWeight = 1f;  // rarity bias
    }

    // Use explicit constructors (works on older Unity/C#)
    public List<Entry> entries = new List<Entry>();

    // Legacy random list (fallback if entries is empty)
    public List<GameObject> cocktailPrefabs = new List<GameObject>();

    // ⭐ NEW: return the whole entry, not just the prefab
    public Entry GetRandomEntry()
    {
        if (entries != null && entries.Count > 0)
        {
            int index = Random.Range(0, entries.Count);
            return entries[index];
        }

        return null;
    }

    // Old method: returns only the prefab
    public GameObject GetRandom()
    {
        if (entries != null && entries.Count > 0)
            return entries[Random.Range(0, entries.Count)].prefab;

        if (cocktailPrefabs == null || cocktailPrefabs.Count == 0) return null;
        return cocktailPrefabs[Random.Range(0, cocktailPrefabs.Count)];
    }

    // Weighted by how close (co,cl,ci) is to each entry’s targets.
    public GameObject GetWeighted(float co, float cl, float ci, float sharpness = 3f)
    {
        if (entries == null || entries.Count == 0) return GetRandom();

        int n = entries.Count;
        float[] weights = new float[n];
        float sum = 0f;

        for (int i = 0; i < n; i++)
        {
            Entry e = entries[i];
            float d   = Mathf.Abs(co - e.targetOld) + Mathf.Abs(cl - e.targetLife) + Mathf.Abs(ci - e.targetImp); // 0..2
            float sim = Mathf.Clamp01(1f - d * 0.5f); // 1..0
            float w   = Mathf.Pow(sim, sharpness) * Mathf.Max(0.0001f, e.baseWeight);
            weights[i] = w;
            sum += w;
        }

        if (sum <= 0f) return GetRandom();

        float r = Random.value * sum;
        for (int i = 0; i < n; i++)
        {
            r -= weights[i];
            if (r <= 0f) return entries[i].prefab;
        }
        return entries[n - 1].prefab;
    }
}
