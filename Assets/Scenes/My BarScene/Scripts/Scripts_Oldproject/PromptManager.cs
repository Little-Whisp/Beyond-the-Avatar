using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PromptManager", menuName = "ScriptableObjects/PromptManager", order = 1)]
public class PromptManager : ScriptableObject
{
    [TextArea] public List<string> agencyPrompts;
    [TextArea] public List<string> experiencePrompts;
    [TextArea] public List<string> prosocialPrompts;
    [TextArea] public List<string> antisocialPrompts;

    private Dictionary<string, List<string>> categories;
    private Dictionary<string, List<int>> usedIndicesPerCategory;

    private void OnEnable()
    {
        categories = new Dictionary<string, List<string>>()
        {
            { "Agency", agencyPrompts },
            { "Experience", experiencePrompts },
            { "Prosocial", prosocialPrompts },
            { "Antisocial", antisocialPrompts }
        };

        usedIndicesPerCategory = new Dictionary<string, List<int>>()
        {
            { "Agency", new List<int>() },
            { "Experience", new List<int>() },
            { "Prosocial", new List<int>() },
            { "Antisocial", new List<int>() }
        };
    }

    // ===============================
    // MAIN METHOD YOU SHOULD USE
    // ===============================
    public string[] GetPromptsForPlayer(int playerIndex)
    {
        bool antisocialFirst = playerIndex % 2 == 0;

        if (antisocialFirst)
        {
            return new string[]
            {
                GetRandomFromCategory("Antisocial"),
                GetRandomFromCategory("Prosocial")
            };
        }
        else
        {
            return new string[]
            {
                GetRandomFromCategory("Prosocial"),
                GetRandomFromCategory("Antisocial")
            };
        }
    }

    // ===============================
    // CATEGORY-BASED RANDOM PICK
    // ===============================
    private string GetRandomFromCategory(string category)
    {
        if (!categories.ContainsKey(category))
            return $"Unknown category: {category}";

        List<string> prompts = categories[category];
        List<int> usedIndices = usedIndicesPerCategory[category];

        if (prompts == null || prompts.Count == 0)
            return $"No prompts in {category}.";

        // Reset when all prompts are used
        if (usedIndices.Count >= prompts.Count)
            usedIndices.Clear();

        int index;
        do
        {
            index = Random.Range(0, prompts.Count);
        }
        while (usedIndices.Contains(index) && usedIndices.Count < prompts.Count);

        usedIndices.Add(index);
        return prompts[index];
    }

    // ===============================
    // OPTIONAL RESET (NEW SESSION)
    // ===============================
    public void ResetAllPromptHistory()
    {
        foreach (var list in usedIndicesPerCategory.Values)
            list.Clear();
    }
}
